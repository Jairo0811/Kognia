# Block 2 — Learning Experience, Assessments and Certificates

Block 2 combines Kognia phases 4, 5 and 6.

## Phase 4 — Learning Experience

Implemented:

- Enrollment in published courses
- Authenticated `My Learning` workspace
- Course player with sections and lesson content
- Per-lesson completion state
- Last-position persistence hook for video/content progress
- Course completion calculation
- Student progress percentage and completed lesson counts
- Access control: learning content requires an enrollment

## Phase 5 — Assessments

Implemented:

- Instructor quiz creation per course
- Configurable passing score
- Multiple-choice question authoring
- Correct-option configuration kept out of student responses
- Draft/published quiz lifecycle
- Student quiz delivery for enrolled courses
- Quiz attempts with automatic scoring
- Attempt history hooks through persisted attempts
- Pass/fail result and best-score projection
- Instructor assessment editor in React

## Phase 6 — Certificates

Implemented:

- Automatic certificate eligibility after course completion
- Published quizzes must all have at least one passing attempt before a certificate is issued
- Courses without required quizzes issue a certificate immediately after all lessons are completed
- Unique public verification code
- Student certificate list
- Public certificate verification endpoint and React verification page
- Certificate uniqueness per student/course

## Persistence

Block 2 adds persistent entities for:

- `Enrollment`
- `LessonProgress`
- `Quiz`
- `Question`
- `QuestionOption`
- `QuizAttempt`
- `QuizAnswer`
- `Certificate`

Migration: `20261003231500_Block2LearningAssessmentCertificates`.

## Tests

Integration coverage includes:

- Learning routes require authentication
- Course enrollment
- Lesson completion and course completion
- Certificate issuance and public verification
- Instructor quiz authoring and publication
- Student quiz payload does not expose correct answers
- Automatic quiz scoring

## Release boundary

PDF rendering/downloading of certificates, richer media telemetry, attempt limits, randomized question banks and commercial anti-cheating controls are intentionally outside this block and can be hardened in later product phases.

Block 2 is ready for promotion only after backend tests, frontend lint/build and the complete CI workflow are green.
