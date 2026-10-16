# RaceDay System — API Endpoint Plan

This table lists every endpoint the RaceDay REST API will expose, planned before any API code is written (Part 2). It is based on the data model in `RaceDay_ERD.png` (Roles, Users, Events, Categories, Enrolments, Results).

**Roles used in this plan:** `None` = public/unauthenticated, `Any (logged in)` = any authenticated user, `Participant`, `Organiser` (usually scoped to the resource's owner), `Admin`.

| # | HTTP Method | Route | Description | Role Required | Request Body | Expected Response |
|---|---|---|---|---|---|---|
| **Authentication** | | | | | | |
| 1 | POST | /api/auth/register | Registers a new user account as either an Organiser or a Participant. | None (public) | `{ fullName, email, password, role }` | `201 Created` — new user id and confirmation.<br>`400 Bad Request` — validation failed.<br>`409 Conflict` — email already registered. |
| 2 | POST | /api/auth/login | Authenticates a user and issues a JWT access token. | None (public) | `{ email, password }` | `200 OK` — JWT token + basic user info.<br>`401 Unauthorized` — invalid credentials. |
| **User Profile** | | | | | | |
| 3 | GET | /api/users/{id} | Retrieves a user's profile details. | Any (logged in), self or Admin | None | `200 OK` — user profile object.<br>`404 Not Found` — user does not exist. |
| 4 | PUT | /api/users/{id} | Updates the logged-in user's own profile details. | Any (logged in), self only | `{ fullName, email }` | `200 OK` — updated profile.<br>`400 Bad Request` — validation failed.<br>`403 Forbidden` — not the profile owner.<br>`404 Not Found`. |
| **Events** | | | | | | |
| 5 | GET | /api/events | Lists all events, with optional filtering (e.g. upcoming only). | None (public) | None | `200 OK` — array of events. |
| 6 | GET | /api/events/{id} | Retrieves the full details of one event. | None (public) | None | `200 OK` — event details.<br>`404 Not Found`. |
| 7 | POST | /api/events | Creates a new event owned by the logged-in organiser. | Organiser | `{ eventName, description, eventDate, location, distanceKm, eventType }` | `201 Created` — new event record.<br>`400 Bad Request` — validation failed. |
| 8 | PUT | /api/events/{id} | Updates an event's details. | Organiser (owner only) | `{ eventName, description, eventDate, location, distanceKm, eventType }` | `200 OK` — updated event.<br>`403 Forbidden` — not the owning organiser.<br>`404 Not Found`. |
| 9 | DELETE | /api/events/{id} | Deletes an event and its categories. | Organiser (owner only) | None | `204 No Content`.<br>`403 Forbidden`.<br>`404 Not Found`. |
| **Categories** | | | | | | |
| 10 | GET | /api/events/{eventId}/categories | Lists all race categories offered under a specific event. | None (public) | None | `200 OK` — array of categories.<br>`404 Not Found` — event does not exist. |
| 11 | POST | /api/events/{eventId}/categories | Adds a new category (e.g. "10km") to an event. | Organiser (owner only) | `{ categoryName, distanceKm, entryFee, maxParticipants }` | `201 Created` — new category record.<br>`403 Forbidden`.<br>`404 Not Found`. |
| 12 | PUT | /api/categories/{id} | Updates a category's details. | Organiser (owner only) | `{ categoryName, distanceKm, entryFee, maxParticipants }` | `200 OK` — updated category.<br>`403 Forbidden`.<br>`404 Not Found`. |
| 13 | DELETE | /api/categories/{id} | Removes a category from an event. | Organiser (owner only) | None | `204 No Content`.<br>`403 Forbidden`.<br>`404 Not Found`. |
| **Event Enrolments** | | | | | | |
| 14 | POST | /api/categories/{id}/enrol | Enrols the logged-in participant into a category. | Participant | None (participant identified from the auth token) | `201 Created` — new enrolment record.<br>`404 Not Found` — category does not exist.<br>`409 Conflict` — category full or already enrolled. |
| 15 | GET | /api/users/{id}/enrolments | Lists all of a user's enrolments across events. | Any (logged in), self or Admin | None | `200 OK` — array of enrolments.<br>`403 Forbidden`.<br>`404 Not Found`. |
| 16 | GET | /api/categories/{id}/enrolments | Lists everyone enrolled in a category (for the organiser to manage). | Organiser (owner only) | None | `200 OK` — array of enrolments.<br>`403 Forbidden`.<br>`404 Not Found`. |
| 17 | DELETE | /api/enrolments/{id} | Cancels/withdraws an enrolment. | Participant (owner) or Organiser (of that event) | None | `204 No Content`.<br>`403 Forbidden`.<br>`404 Not Found`. |
| **Results** | | | | | | |
| 18 | POST | /api/enrolments/{id}/result | Captures a finishing result for a completed enrolment. | Organiser (owner only) | `{ finishTime, position, status }` | `201 Created` — new result record.<br>`403 Forbidden`.<br>`404 Not Found`.<br>`409 Conflict` — result already recorded. |
| 19 | PUT | /api/results/{id} | Corrects a previously captured result. | Organiser (owner only) | `{ finishTime, position, status }` | `200 OK` — updated result.<br>`403 Forbidden`.<br>`404 Not Found`. |
| 20 | GET | /api/categories/{id}/results | Retrieves the results/leaderboard for a category, ordered by position. | None (public) | None | `200 OK` — array of results.<br>`404 Not Found`. |
| 21 | GET | /api/results/{id} | Retrieves a single result record. | None (public) | None | `200 OK` — result details.<br>`404 Not Found`. |

## Notes
- Every mutating endpoint (`POST`/`PUT`/`DELETE`) requires a valid JWT; role checks are enforced in addition to authentication.
- "Owner only" means the logged-in Organiser must be the `OrganiserId` on the parent `Event` — this is checked in the API layer, not just the database.
- This plan covers the minimum functionality (Authentication, User Profile, Events, Categories, Event Enrolments, Results) plus update/delete endpoints identified as necessary for a usable system.
- The implemented API in Part 2 is expected to match this plan closely; any deviation will be explained in the README.
