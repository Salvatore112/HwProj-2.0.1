import * as React from "react";
import { TextField, Button, Typography } from "@material-ui/core";
import ApiSingleton from "../../api/ApiSingleton";
import { FC, FormEvent, useState, useEffect } from "react";
import GroupIcon from '@material-ui/icons/Group';
import makeStyles from "@material-ui/styles/makeStyles";
import Container from "@material-ui/core/Container";
import Autocomplete from "@material-ui/lab/Autocomplete";

interface ICreateCourseState {
  name: string;
  groupName?: string;
  courseId: string;
  errors: string[];
}

const useStyles = makeStyles((theme) => ({
  paper: {
    marginTop: theme.spacing(7),
    display: 'flex',
    flexDirection: 'column',
    alignItems: 'center',
    padding: theme.spacing(3),
    backgroundColor: theme.palette.background.paper,
  },
  avatar: {
    margin: theme.spacing(1),
  },
  form: {
    marginTop: theme.spacing(3),
    width: '100%',
  },
  button: {
    marginTop: theme.spacing(1),
  },
}));

const CreateCourse: FC = () => {
  const [course, setCourse] = useState<ICreateCourseState>({
    name: "",
    groupName: "",
    courseId: "",
    errors: [],
  });

  const [programNames, setProgramNames] = useState<string[]>([]);
  const [programName, setProgramName] = useState<string>('');
  const [groupNames, setGroupNames] = useState<string[]>([]);
  const [fetchingGroups, setFetchingGroups] = useState<boolean>(false);

  useEffect(() => {
    const fetchProgramNames = async () => {
      try {
        const response = await ApiSingleton.coursesApi.apiCoursesGetProgramNamesGet();
        const data = await response.json();
        setProgramNames(data);
      } catch (e) {
        console.error("Error fetching program names:", e);
        setProgramNames([]);
      }
    };

    fetchProgramNames();
  }, []);

  useEffect(() => {
    const fetchGroups = async (program: string) => {
      if (!program) return;
      setFetchingGroups(true);
      try {
        const response = await ApiSingleton.coursesApi.apiCoursesGetGroupsGet(program);
        const data = await response.json();
        setGroupNames(data);
      } catch (e) {
        console.error("Error fetching groups:", e);
        setGroupNames([]);
      } finally {
        setFetchingGroups(false);
      }
    };

    if (programName) {
      fetchGroups(programName);
    } else {
      setGroupNames([]);
    }
  }, [programName]);

  useEffect(() => {
    if (course.groupName) {
      const fetchStudents = async () => {
        try {
          const response = await ApiSingleton.coursesApi.apiCoursesGetStudentStsGet(course.groupName);
          const students = await response.json();
          console.log(students); 
        } catch (error) {
          console.error('Error fetching students:', error);
        }
      };

      fetchStudents();
    }
  }, [course.groupName]); 

  const handleSubmit = async (e: FormEvent<HTMLFormElement>) => {
    e.preventDefault();
    const courseViewModel = {
      name: course.name,
      groupName: course.groupName,
      isOpen: true,
    };
    try {
      const courseId = await ApiSingleton.coursesApi.apiCoursesCreatePost(courseViewModel);
      setCourse((prevState) => ({
        ...prevState,
        courseId: courseId.toString(),
      }));
      window.location.assign("/");
    } catch (e) {
      setCourse((prevState) => ({
        ...prevState,
        errors: ['Сервис недоступен'],
      }));
    }
  };

  const classes = useStyles();

  if (!ApiSingleton.authService.isLecturer()) {
    return (
      <Typography component="h1" variant="h5">
        Страница не доступна
      </Typography>
    );
  }

  return (
    <Container component="main" maxWidth="xs">
      <div className={classes.paper}>
        <GroupIcon
          fontSize="large"
          style={{ color: 'white', backgroundColor: '#ecb50d' }}
          className={classes.avatar}
        />
        <Typography component="h1" variant="h5">
          Создать курс
        </Typography>

        <form onSubmit={(e) => handleSubmit(e)} className={classes.form}>
          <TextField
            required
            label="Название курса"
            variant="outlined"
            fullWidth
            name={course.name}
            onChange={(e) => {
              e.persist();
              setCourse((prevState) => ({
                ...prevState,
                name: e.target.value,
              }));
            }}
          />

          <Autocomplete
            value={programName}
            onChange={(event, newValue) => {
              setProgramName(newValue || '');
            }}
            options={programNames}
            renderInput={(params) => (
              <TextField
                {...params}
                label="Название программы"
                variant="outlined"
                fullWidth
                style={{ marginTop: '16px' }}
              />
            )}
            fullWidth
          />

          <Autocomplete
            value={course.groupName}
            onChange={(event, newValue) => {
              setCourse((prevState) => ({
                ...prevState,
                groupName: newValue || '',
              }));
            }}
            options={groupNames}
            renderInput={(params) => (
              <TextField
                {...params}
                label="Название группы"
                variant="outlined"
                fullWidth
                style={{ marginTop: '16px' }}
              />
            )}
            fullWidth
          />

          <Button
            fullWidth
            variant="contained"
            color="primary"
            type="submit"
            style={{ marginTop: '16px' }}
          >
            Создать курс
          </Button>
        </form>
      </div>
    </Container>
  );
};

export default CreateCourse;
