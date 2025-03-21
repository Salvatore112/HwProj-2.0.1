using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using HwProj.Models.SolutionsService;
using HwProj.SolutionsService.API.Models;
using Octokit;

namespace HwProj.SolutionsService.API.Domains
{
    internal static class SolutionHelper
    {
        private static Regex _pullRequestRegex = new Regex(
            @"https:\/\/github\.com\/(?<owner>[^\/]+)\/(?<repo>[^\/]+)\/pull\/(?<number>\d+)(\/.*)?",
            RegexOptions.Compiled);

        public static PullRequestDto? TryParsePullRequestUrl(string url)
        {
            if (url is null)
                throw new ArgumentNullException(nameof(url));

            var match = _pullRequestRegex.Match(url);

            if (match.Success)
            {
                return new PullRequestDto
                {
                    Owner = match.Groups["owner"].Value,
                    RepoName = match.Groups["repo"].Value,
                    Number = int.Parse(match.Groups["number"].Value),
                };
            }

            return null;
        }

        public static SolutionActualityPart GetCommitActuality(
            IEnumerable<PullRequestCommit> pullRequestCommits,
            GithubSolutionCommit? lastSolutionCommit)
        {
            var pullRequestCommitsSha = pullRequestCommits.Select(c => c.Sha).ToHashSet();

            var comment = string.Empty;

            if (lastSolutionCommit == null)
                comment = "Для данного решения не была сохранена информация о коммитах";
            else if (pullRequestCommitsSha.Count == 0)
                comment = "В ветке были удалены коммиты. Возможно, был произведен force push";
            else if (!pullRequestCommitsSha.Contains(lastSolutionCommit.CommitHash))
                comment = "Последний коммит решения в текущей ветке не найден. Возможно, был произведен force push";
            else if (pullRequestCommitsSha.Last() != lastSolutionCommit.CommitHash)
                comment = "С момента сдачи последнего решения были добавлены новые коммиты";

            return new SolutionActualityPart
            {
                isActual = comment == string.Empty,
                Comment = comment,
                AdditionalData = lastSolutionCommit?.CommitHash ?? ""
            };
        }
    }
}
