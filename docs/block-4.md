# Block 4 — Admin Portal, Reviews & Favorites, Notifications

Block 4 combines Kognia phases 10, 11 and 12.

## Phase 10 — Admin Portal

Implemented:

- Administrator-only API group
- Operational dashboard summary
- User listing and role management
- Course moderation with Draft / Published / Archived lifecycle
- Review moderation and visibility controls
- Global notification broadcast
- React administrative portal

## Phase 11 — Reviews & Favorites

Implemented:

- Public visible review list per course
- One review per student/course
- Reviews restricted to enrolled students
- 1–5 rating validation
- Review update and delete support
- Moderation visibility flag
- Favorite/unfavorite published courses
- Personal favorites list
- React engagement discovery, favorites and review screens

## Phase 12 — Notifications

Implemented:

- Persistent user notifications
- Notification categories
- Read/unread state and timestamps
- Per-user feed with unread count
- Mark one or all notifications as read
- Instructor notification when a student reviews a course
- Administrator broadcast to all registered users
- React notification center

## Persistence

Block 4 adds:

- `Review`
- `Favorite`
- `Notification`

Migration: `20261004002000_Block4AdminEngagementNotifications`.

## Security and authorization

- Admin endpoints require the `Administrator` role.
- Instructor review notifications are scoped to the course owner.
- Notification read operations are scoped to the authenticated user.
- Students cannot review courses without an enrollment.
- Favorites only accept published courses.

## Tests

Integration coverage includes:

- Favorite creation/listing
- Review creation by an enrolled student
- Public review retrieval
- Instructor notification creation after a review
- Notification read flow
- Administrator authorization boundary
- Admin dashboard/users/courses access
- Global notification broadcast

## Release boundary

Advanced moderation workflows, audit trails, abuse reporting, notification delivery through email/push, batching, digest preferences and real-time SignalR delivery remain candidates for later hardening/engagement iterations.

Block 4 is ready for promotion only after backend tests, frontend lint/build and the complete CI workflow are green.
