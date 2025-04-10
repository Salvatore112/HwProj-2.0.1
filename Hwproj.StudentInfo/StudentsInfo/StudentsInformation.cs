﻿using System;
using System.Collections.Generic;
using System.Linq;
using HtmlAgilityPack;
using Novell.Directory.Ldap;
using System.Threading.Tasks;
using IStudentsInfo;

namespace StudentsInfo
{
    public class StudentsInformationProvider : IStudentsInformationProvider
    {
        private readonly Lazy<Dictionary<string, List<string>>> _lazyProgramsGroups;
        private readonly string _ldapHost = "ad.pu.ru";
        private readonly int _ldapPort = 389;
        private readonly string _searchBase = "DC=ad,DC=pu,DC=ru";
        private string _username;
        private string _password;
        
        public async Task<List<GroupModel>> GetGroups(string programName)
        {
            return await Task.Run(() => 
            {
                return _lazyProgramsGroups.Value.ContainsKey(programName)
                    ? _lazyProgramsGroups.Value[programName]
                        .Aggregate((current, next) => current + "," + next) 
                        .Split(',')
                        .Select(group => new GroupModel { GroupName = group.Trim() })
                        .ToList()
                    : new List<GroupModel>();
            });
        }
        
        public List<StudentModel> GetStudentInformation(string groupName)
        {
            var searchFilter = $"(&(objectClass=person)(memberOf=CN=АкадемГруппа_{groupName},OU=АкадемГруппа,OU=Группы,DC=ad,DC=pu,DC=ru))";
            var studentsList = new List<StudentModel>();
            LdapConnection connection = null;
            
            try
            {
                connection = new LdapConnection();
                connection.Connect(_ldapHost, _ldapPort);
                connection.Bind(_username, _password);

                var results = connection.Search(
                    _searchBase,
                    LdapConnection.SCOPE_SUB,
                    searchFilter,
                    new[] { "cn", "displayName" },
                    false
                );

                while (results.hasMore())
                {
                    var entry = results.next();
                    var cn = entry.getAttribute("cn")?.StringValue;
                    var displayName = entry.getAttribute("displayName")?.StringValue;
                    
                    if (cn != null && displayName != null)
                    {
                        string[] splitNames = displayName.Split(' ');
                        var newStudent = new StudentModel
                        {
                            Name = splitNames[0],
                            Surname = splitNames.Length > 1 ? splitNames[1] : "",
                            MiddleName = splitNames.Length > 2 ? splitNames[2] : "",
                            Email = cn + "@student.spbu.ru"
                        };
                        studentsList.Add(newStudent);
                    }
                }
            }
            catch (LdapReferralException)
            {
            }
            catch (LdapException ldapEx)
            {
                Console.WriteLine($"LDAP Error: {ldapEx.Message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
            finally
            {
                try
                {
                    if (connection != null && connection.Connected)
                    {
                            SafeDisconnect(connection);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error during disconnect: {ex.Message}");
                }
            }

            return studentsList;
        }

        private void SafeDisconnect(LdapConnection connection)
        {
            try
            {
                connection.Disconnect();
            }
            catch (PlatformNotSupportedException)
            {
            }
            catch (Exception ex)
            {
                Console.WriteLine($"SafeDisconnect error: {ex.Message}");
            }
        }
        
        public List<ProgramModel> GetProgramNames()
        {
            return _lazyProgramsGroups.Value.Keys
                .Select(key => new ProgramModel { ProgramName = key })
                .ToList();
        }

        public StudentsInformationProvider(string username, string password, string ldapHost, int ldapPort, string searchBase)
        {
            this._username = username;
            this._password = password;
            this._ldapHost = ldapHost;
            this._ldapPort = ldapPort;
            this._searchBase = searchBase;

            _lazyProgramsGroups = new Lazy<Dictionary<string, List<string>>>(() =>
            {
                var programsGroups = new Dictionary<string, List<string>>();
                
                try
                {
                    const string url = "https://timetable.spbu.ru/MATH?lang=ru";
                    var web = new HtmlWeb();

                    web.PreRequest = request =>
                    {
                        request.Headers.Add("Accept-Language", "ru");
                        return true;
                    };

                    var doc = web.Load(url);
                    var programNodes = doc.DocumentNode.SelectNodes("//li[contains(@class, 'common-list-item row')]");

                    foreach (var programNode in programNodes)
                    {
                        var programNameNode = programNode.SelectSingleNode(".//div[contains(@class, 'col-sm-5')]");
                        var programName = programNameNode?.InnerText.Trim();

                        var titleNodes = programNode.SelectNodes(".//div[contains(@class, 'col-sm-1')]");

                        if (titleNodes != null && programName != null)
                        {
                            var titles = new List<string>();
                            foreach (var titleNode in titleNodes)
                            {
                                var title = titleNode.SelectSingleNode(".//a")?.Attributes["title"]?.Value;
                                if (title != null)
                                {
                                    titles.Add(title);
                                }
                            }

                            if (programsGroups.ContainsKey(programName))
                            {
                                programsGroups[programName].AddRange(titles);
                            }
                            else
                            {
                                programsGroups[programName] = titles;
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error loading programs: {ex.Message}");
                }

                return programsGroups;
            });
        }
    }
}
