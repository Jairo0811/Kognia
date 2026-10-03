import { FormEvent, useEffect, useState } from 'react';
import { Link, Navigate, useParams } from 'react-router-dom';
import { api, getSession } from './lib/api';

type MyCourse = {
  enrollmentId: string;
  courseId: string;
  title: string;
  slug: string;
  summary: string;
  level: string;
  status: number;
  totalLessons: number;
  completedLessons: number;
  progressPercent: number;
};

type LessonProgress = {
  isCompleted: boolean;
  lastPositionSeconds: number;
  lastAccessedAtUtc: string;
  completedAtUtc?: string;
};

type LearningLesson = {
  id: string;
  title: string;
  lessonType: string;
  content?: string;
  videoUrl?: string;
  position: number;
  isPreview: boolean;
};

type LearningCourse = {
  course: {
    id: string;
    title: string;
    slug: string;
    summary: string;
    description: string;
    level: string;
    sections: Array<{ id: string; title: string; position: number; lessons: LearningLesson[] }>;
  };
  enrollment: { id: string; status: number; enrolledAtUtc: string; completedAtUtc?: string };
  progress: Record<string, LessonProgress>;
  quizzes: Array<{ id: string; title: string; passingScorePercent: number; questions: number; bestScore?: number; passed: boolean }>;
};

type Quiz = {
  id: string;
  courseId: string;
  title: string;
  passingScorePercent: number;
  questions: Array<{
    id: string;
    text: string;
    position: number;
    options: Array<{ id: string; text: string; position: number }>;
  }>;
};

type QuizResult = {
  id: string;
  scorePercent: number;
  passed: boolean;
  correctAnswers: number;
  totalQuestions: number;
  certificate?: { id: string; verificationCode: string; issuedAtUtc: string };
};

type Certificate = {
  id: string;
  courseId: string;
  courseTitle: string;
  verificationCode: string;
  issuedAtUtc: string;
};

type InstructorQuizSummary = {
  id: string;
  title: string;
  passingScorePercent: number;
  isPublished: boolean;
  questions: number;
  attempts: number;
};

type InstructorQuiz = {
  id: string;
  courseId: string;
  title: string;
  passingScorePercent: number;
  isPublished: boolean;
  questions: Array<{
    id: string;
    text: string;
    position: number;
    options: Array<{ id: string; text: string; isCorrect: boolean; position: number }>;
  }>;
};

function Block2Nav() {
  return (
    <nav aria-label="Navegación de aprendizaje">
      <Link to="/">Kognia</Link>{' '}
      <Link to="/courses">Cursos</Link>{' '}
      <Link to="/my-learning">Mi aprendizaje</Link>{' '}
      <Link to="/certificates">Certificados</Link>
    </nav>
  );
}

export function MyLearningPage() {
  const session = getSession();
  const [courses, setCourses] = useState<MyCourse[]>([]);
  const [message, setMessage] = useState('');

  useEffect(() => {
    if (!session) return;
    api<MyCourse[]>('/api/learning/me/courses')
      .then(setCourses)
      .catch(() => setMessage('No fue posible cargar tus cursos.'));
  }, [session]);

  if (!session) return <Navigate to="/login" replace />;

  return (
    <>
      <Block2Nav />
      <main>
        <h1>Mi aprendizaje</h1>
        {message && <p role="alert">{message}</p>}
        {courses.length === 0 ? <p>Aún no estás inscrito en ningún curso.</p> : (
          <ul>
            {courses.map((course) => (
              <li key={course.enrollmentId}>
                <h2>{course.title}</h2>
                <p>{course.summary}</p>
                <progress value={course.progressPercent} max={100}>{course.progressPercent}%</progress>
                <p>{course.completedLessons}/{course.totalLessons} lecciones · {course.progressPercent}%</p>
                <Link to={`/learn/${course.courseId}`}>Continuar aprendiendo</Link>
              </li>
            ))}
          </ul>
        )}
      </main>
    </>
  );
}

export function LearningCoursePage() {
  const session = getSession();
  const { courseId } = useParams();
  const [data, setData] = useState<LearningCourse | null>(null);
  const [message, setMessage] = useState('');

  async function load() {
    if (!courseId) return;
    try {
      setData(await api<LearningCourse>(`/api/learning/courses/${courseId}`));
    } catch {
      setMessage('No fue posible abrir este curso. Verifica que estés inscrito.');
    }
  }

  useEffect(() => {
    if (session && courseId) void load();
  }, [courseId]);

  if (!session) return <Navigate to="/login" replace />;
  if (!courseId) return <Navigate to="/my-learning" replace />;

  async function toggleLesson(lesson: LearningLesson) {
    const completed = data?.progress[lesson.id]?.isCompleted ?? false;
    const result = await api<{ progressPercent: number; courseCompleted: boolean; certificate?: { verificationCode: string } }>(
      `/api/learning/lessons/${lesson.id}/progress`,
      { method: 'PUT', body: JSON.stringify({ lastPositionSeconds: 0, isCompleted: !completed }) },
    );
    if (result.certificate) setMessage(`¡Curso completado! Certificado emitido: ${result.certificate.verificationCode}`);
    else if (result.courseCompleted) setMessage('Curso completado. Aprueba las evaluaciones publicadas para recibir el certificado.');
    else setMessage(`Progreso actualizado: ${result.progressPercent}%`);
    await load();
  }

  return (
    <>
      <Block2Nav />
      <main>
        {!data ? <p>{message || 'Cargando curso...'}</p> : (
          <>
            <h1>{data.course.title}</h1>
            <p>{data.course.description}</p>
            {message && <p role="status">{message}</p>}
            {data.course.sections.map((section) => (
              <section key={section.id}>
                <h2>{section.position}. {section.title}</h2>
                {section.lessons.map((lesson) => {
                  const completed = data.progress[lesson.id]?.isCompleted ?? false;
                  return (
                    <article key={lesson.id}>
                      <h3>{lesson.position}. {lesson.title}</h3>
                      <p>Tipo: {lesson.lessonType}</p>
                      {lesson.content && <p>{lesson.content}</p>}
                      {lesson.videoUrl && <p><a href={lesson.videoUrl} target="_blank" rel="noreferrer">Abrir video</a></p>}
                      <button type="button" onClick={() => void toggleLesson(lesson)}>
                        {completed ? 'Marcar como pendiente' : 'Marcar como completada'}
                      </button>
                    </article>
                  );
                })}
              </section>
            ))}

            <section>
              <h2>Evaluaciones</h2>
              {data.quizzes.length === 0 ? <p>No hay evaluaciones publicadas.</p> : (
                <ul>{data.quizzes.map((quiz) => (
                  <li key={quiz.id}>
                    {quiz.title} · Mínimo {quiz.passingScorePercent}% · {quiz.passed ? 'Aprobada' : 'Pendiente'}{' '}
                    <Link to={`/quiz/${quiz.id}`}>{quiz.passed ? 'Repetir' : 'Realizar evaluación'}</Link>
                  </li>
                ))}</ul>
              )}
            </section>
          </>
        )}
      </main>
    </>
  );
}

export function QuizPage() {
  const session = getSession();
  const { quizId } = useParams();
  const [quiz, setQuiz] = useState<Quiz | null>(null);
  const [answers, setAnswers] = useState<Record<string, string>>({});
  const [result, setResult] = useState<QuizResult | null>(null);
  const [message, setMessage] = useState('');

  useEffect(() => {
    if (!session || !quizId) return;
    api<Quiz>(`/api/learning/quizzes/${quizId}`)
      .then(setQuiz)
      .catch(() => setMessage('No fue posible cargar la evaluación.'));
  }, [quizId]);

  if (!session) return <Navigate to="/login" replace />;
  if (!quizId) return <Navigate to="/my-learning" replace />;

  async function submit(event: FormEvent) {
    event.preventDefault();
    if (!quiz) return;
    const payload = {
      answers: quiz.questions.map((question) => ({ questionId: question.id, selectedOptionId: answers[question.id] ?? null })),
    };
    try {
      setResult(await api<QuizResult>(`/api/learning/quizzes/${quiz.id}/attempts`, { method: 'POST', body: JSON.stringify(payload) }));
      setMessage('Evaluación enviada.');
    } catch {
      setMessage('No fue posible enviar la evaluación.');
    }
  }

  return (
    <>
      <Block2Nav />
      <main>
        {!quiz ? <p>{message || 'Cargando evaluación...'}</p> : (
          <>
            <h1>{quiz.title}</h1>
            <p>Puntuación mínima: {quiz.passingScorePercent}%</p>
            <form onSubmit={submit}>
              {quiz.questions.map((question) => (
                <fieldset key={question.id}>
                  <legend>{question.position}. {question.text}</legend>
                  {question.options.map((option) => (
                    <label key={option.id}>
                      <input
                        type="radio"
                        name={`question-${question.id}`}
                        value={option.id}
                        checked={answers[question.id] === option.id}
                        onChange={() => setAnswers((current) => ({ ...current, [question.id]: option.id }))}
                        required
                      /> {option.text}
                    </label>
                  ))}
                </fieldset>
              ))}
              <button type="submit">Enviar evaluación</button>
            </form>
            {message && <p role="status">{message}</p>}
            {result && (
              <section>
                <h2>Resultado</h2>
                <p>{result.scorePercent}% · {result.passed ? 'Aprobada' : 'No aprobada'}</p>
                <p>{result.correctAnswers} de {result.totalQuestions} respuestas correctas.</p>
                {result.certificate && <p>Certificado emitido: <Link to="/certificates">{result.certificate.verificationCode}</Link></p>}
                <Link to={`/learn/${quiz.courseId}`}>Volver al curso</Link>
              </section>
            )}
          </>
        )}
      </main>
    </>
  );
}

export function CertificatesPage() {
  const session = getSession();
  const [certificates, setCertificates] = useState<Certificate[]>([]);
  const [message, setMessage] = useState('');

  useEffect(() => {
    if (!session) return;
    api<Certificate[]>('/api/certificates/me')
      .then(setCertificates)
      .catch(() => setMessage('No fue posible cargar tus certificados.'));
  }, [session]);

  if (!session) return <Navigate to="/login" replace />;

  return (
    <>
      <Block2Nav />
      <main>
        <h1>Mis certificados</h1>
        {message && <p role="alert">{message}</p>}
        {certificates.length === 0 ? <p>Aún no tienes certificados emitidos.</p> : (
          <ul>{certificates.map((certificate) => (
            <li key={certificate.id}>
              <h2>{certificate.courseTitle}</h2>
              <p>Código: {certificate.verificationCode}</p>
              <p>Emitido: {new Date(certificate.issuedAtUtc).toLocaleDateString()}</p>
              <Link to={`/certificates/verify/${certificate.verificationCode}`}>Verificar certificado</Link>
            </li>
          ))}</ul>
        )}
      </main>
    </>
  );
}

export function CertificateVerifyPage() {
  const { verificationCode } = useParams();
  const [data, setData] = useState<{ valid: boolean; certificate?: { verificationCode: string; issuedAtUtc: string; courseTitle: string; student?: { firstName: string; lastName: string } } } | null>(null);

  useEffect(() => {
    if (!verificationCode) return;
    api<{ valid: boolean; certificate: { verificationCode: string; issuedAtUtc: string; courseTitle: string; student?: { firstName: string; lastName: string } } }>(
      `/api/certificates/verify/${encodeURIComponent(verificationCode)}`,
    ).then(setData).catch(() => setData({ valid: false }));
  }, [verificationCode]);

  return (
    <>
      <Block2Nav />
      <main>
        <h1>Verificación de certificado</h1>
        {!data ? <p>Verificando...</p> : !data.valid ? <p>Certificado no válido o no encontrado.</p> : (
          <section>
            <p><strong>Certificado válido</strong></p>
            <p>Curso: {data.certificate?.courseTitle}</p>
            <p>Estudiante: {data.certificate?.student ? `${data.certificate.student.firstName} ${data.certificate.student.lastName}` : 'Estudiante Kognia'}</p>
            <p>Código: {data.certificate?.verificationCode}</p>
            <p>Emitido: {data.certificate?.issuedAtUtc ? new Date(data.certificate.issuedAtUtc).toLocaleDateString() : ''}</p>
          </section>
        )}
      </main>
    </>
  );
}

export function InstructorAssessmentsPage() {
  const session = getSession();
  const { courseId } = useParams();
  const [quizzes, setQuizzes] = useState<InstructorQuizSummary[]>([]);
  const [selected, setSelected] = useState<InstructorQuiz | null>(null);
  const [title, setTitle] = useState('');
  const [passingScorePercent, setPassingScorePercent] = useState(70);
  const [questionText, setQuestionText] = useState('');
  const [options, setOptions] = useState(['', '', '', '']);
  const [correctIndex, setCorrectIndex] = useState(0);
  const [message, setMessage] = useState('');

  const canManage = session?.roles.some((role) => role === 'Instructor' || role === 'Administrator') ?? false;

  async function load() {
    if (!courseId) return;
    setQuizzes(await api<InstructorQuizSummary[]>(`/api/instructor/courses/${courseId}/quizzes`));
  }

  useEffect(() => {
    if (canManage && courseId) void load();
  }, [canManage, courseId]);

  if (!session) return <Navigate to="/login" replace />;
  if (!canManage || !courseId) return <Navigate to="/instructor" replace />;

  async function createQuiz(event: FormEvent) {
    event.preventDefault();
    try {
      await api(`/api/instructor/courses/${courseId}/quizzes`, {
        method: 'POST',
        body: JSON.stringify({ title, passingScorePercent }),
      });
      setTitle('');
      setMessage('Evaluación creada como borrador.');
      await load();
    } catch {
      setMessage('No fue posible crear la evaluación.');
    }
  }

  async function openQuiz(id: string) {
    setSelected(await api<InstructorQuiz>(`/api/instructor/quizzes/${id}`));
  }

  async function refreshSelected() {
    if (selected) setSelected(await api<InstructorQuiz>(`/api/instructor/quizzes/${selected.id}`));
  }

  async function addQuestion(event: FormEvent) {
    event.preventDefault();
    if (!selected) return;
    const prepared = options.map((text, index) => ({ text, isCorrect: index === correctIndex, position: index + 1 })).filter((x) => x.text.trim());
    if (prepared.length < 2 || !prepared.some((x) => x.isCorrect)) {
      setMessage('Agrega al menos dos opciones y selecciona una respuesta correcta disponible.');
      return;
    }
    await api(`/api/instructor/quizzes/${selected.id}/questions`, {
      method: 'POST',
      body: JSON.stringify({ text: questionText, position: selected.questions.length + 1, options: prepared }),
    });
    setQuestionText('');
    setOptions(['', '', '', '']);
    setCorrectIndex(0);
    setMessage('Pregunta agregada.');
    await Promise.all([refreshSelected(), load()]);
  }

  async function publishQuiz() {
    if (!selected) return;
    try {
      await api(`/api/instructor/quizzes/${selected.id}/publish`, { method: 'POST' });
      setMessage('Evaluación publicada.');
      await Promise.all([refreshSelected(), load()]);
    } catch {
      setMessage('La evaluación requiere preguntas válidas antes de publicarse.');
    }
  }

  return (
    <>
      <Block2Nav />
      <main>
        <h1>Evaluaciones del curso</h1>
        <p><Link to="/instructor">Volver al portal del instructor</Link></p>
        {message && <p role="status">{message}</p>}

        <form onSubmit={createQuiz}>
          <h2>Nueva evaluación</h2>
          <label>Título<input value={title} onChange={(e) => setTitle(e.target.value)} required /></label>
          <label>Puntuación mínima<input type="number" min={1} max={100} value={passingScorePercent} onChange={(e) => setPassingScorePercent(Number(e.target.value))} required /></label>
          <button type="submit">Crear evaluación</button>
        </form>

        <section>
          <h2>Evaluaciones</h2>
          {quizzes.length === 0 ? <p>No hay evaluaciones todavía.</p> : (
            <ul>{quizzes.map((quiz) => (
              <li key={quiz.id}>{quiz.title} · {quiz.questions} preguntas · {quiz.isPublished ? 'Publicada' : 'Borrador'}{' '}
                <button type="button" onClick={() => void openQuiz(quiz.id)}>Editar</button>
              </li>
            ))}</ul>
          )}
        </section>

        {selected && (
          <section>
            <h2>Editor: {selected.title}</h2>
            <p>Mínimo: {selected.passingScorePercent}% · {selected.isPublished ? 'Publicada' : 'Borrador'}</p>
            <button type="button" disabled={selected.isPublished} onClick={() => void publishQuiz()}>Publicar evaluación</button>
            {selected.questions.length > 0 && (
              <ol>{selected.questions.map((question) => (
                <li key={question.id}>{question.text}
                  <ul>{question.options.map((option) => <li key={option.id}>{option.text}{option.isCorrect ? ' ✓' : ''}</li>)}</ul>
                </li>
              ))}</ol>
            )}

            {!selected.isPublished && (
              <form onSubmit={addQuestion}>
                <h3>Agregar pregunta</h3>
                <label>Pregunta<textarea value={questionText} onChange={(e) => setQuestionText(e.target.value)} required /></label>
                {options.map((option, index) => (
                  <label key={index}>Opción {index + 1}
                    <input value={option} onChange={(e) => setOptions((current) => current.map((value, currentIndex) => currentIndex === index ? e.target.value : value))} />
                  </label>
                ))}
                <label>Respuesta correcta
                  <select value={correctIndex} onChange={(e) => setCorrectIndex(Number(e.target.value))}>
                    {options.map((_, index) => <option key={index} value={index}>Opción {index + 1}</option>)}
                  </select>
                </label>
                <button type="submit">Agregar pregunta</button>
              </form>
            )}
          </section>
        )}
      </main>
    </>
  );
}
