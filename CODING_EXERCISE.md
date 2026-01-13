# Coding Exercise Challenges

## Overview

Welcome to the Regtransfers coding exercise! This repository contains a working microservices application for managing UK number plate inventory. Your task is to complete ONE of the following challenges.

**Choose Your Challenge:** Pick the challenge that interests you most. Each challenge is designed to be equally challenging but tests different aspects of software development. We're interested in seeing how you approach the problem, not just whether you complete it.

**Choose Your Frontend:** The application has two frontend implementations (ASP.NET MVC and Angular). **You only need to implement your chosen challenge in ONE of these frontends** - whichever you're most comfortable with. We're not expecting you to duplicate work across both tech stacks.

**Time Allocation:** We expect this exercise to take 3-4 hours maximum. We value quality over completion - it's better to partially complete a challenge with excellent code than to rush through everything.

**Submission:** 
1. Fork this repository
2. Complete your chosen challenge
3. Submit a link to your fork
4. Include a brief `SOLUTION.md` explaining:
   - Which challenge you chose and why
   - Your approach and key decisions
   - What you'd do differently with more time
   - Any trade-offs you made

---

## Challenge 1: Implement Plate Watchlist with Notifications

**User Story:**
```gherkin
As a customer
I want to add number plates to a personal watchlist with price alerts
So that I can be notified when watched plates become available or drop in price
```

**Additional Context:**
- Each customer can have multiple plates in their watchlist with individual price alert thresholds
- Customers should be able to add and remove plates from their watchlist  
- The watchlist should be visible in both the MVC and Angular frontends
- Watchlist data should persist in the database
- When a plate status changes (e.g., reserved → for sale), customers should be notified
- When a plate price drops below a customer's alert threshold, trigger a notification
- Must integrate with the existing event bus (MassTransit/RabbitMQ)
- Consider scalability - what if 1000 customers are watching the same plate?
- Think about notification delivery mechanisms (in-app, future email/SMS hooks)
- Consider stale data - what if a customer's watchlist shows a plate that was just sold?

**Time Estimate:** 3-4 hours

**Acceptance Criteria:**
```gherkin
Scenario: Add plate to watchlist with price alert
  Given I am viewing a number plate priced at £5000
  When I click "Add to Watchlist"
  And I set a price alert for £4500
  Then the plate should be added to my watchlist
  And I should see a confirmation message
  And the button should change to "Remove from Watchlist"

Scenario: View my watchlist with status indicators
  Given I have plates in my watchlist
  When I navigate to the watchlist page
  Then I should see all plates I've added
  And each plate should display its current registration, price, and status
  And plates that are no longer available should be visually distinct

Scenario: Receive notification when watched plate status changes
  Given I have a reserved plate in my watchlist
  When that plate becomes available for sale
  Then I should receive a notification
  And the notification should appear in the UI
  And the watchlist should update to show the new status

Scenario: Receive notification when price drops below alert threshold
  Given I have a plate in my watchlist with a £4500 price alert
  When the plate price is reduced to £4200
  Then I should receive a price alert notification
  And the notification should show the old and new price

Scenario: Handle high-volume watchlist updates efficiently
  Given 500 customers are watching the same plate
  When that plate's price changes
  Then all 500 customers should be notified
  And the system should handle this without performance degradation

Scenario: Watchlist synchronization across sessions
  Given I have added plates to my watchlist
  When I close and reopen the application in a different browser
  Then my watchlist should still contain those plates
  And should reflect any status/price changes that occurred while I was away
```

**Technical Requirements:**
- Add `Watchlist` and `Notification` entities with appropriate relationships
- Create RESTful API endpoints for watchlist and notification operations
- **Integrate with existing event bus** - consume `PlateSoldIntegrationEvent`, `PlateReservedIntegrationEvent`, etc.
- Publish new events when watchlist conditions are met (e.g., `PriceAlertTriggeredEvent`)
- Implement notification delivery mechanism (in-app notifications, with hooks for future email/SMS)
- Update **ONE frontend application** (your choice of MVC or Angular) with:
  - Watchlist management UI
  - Real-time notification display
  - Price alert configuration
- Add appropriate unit and integration tests
- Follow existing patterns and architecture in the codebase
- Consider scalability - efficient queries when many users watch the same plates
- Handle race conditions and stale data scenarios
- Implement proper error handling for event processing failures

**What We're Looking For:**
- Event-driven architecture using existing MassTransit/RabbitMQ infrastructure
- Proper separation of concerns between watchlist and notification domains
- Scalable design for high-volume scenarios (many users watching popular plates)
- Thoughtful notification strategy (frequency, grouping, persistence)
- Clean API design with appropriate status codes and error handling
- Real-time or near-real-time UI updates
- Testing strategy including event-driven scenarios
- Documentation of architectural decisions and trade-offs

---

## Challenge 2: Implement Advanced Search with Fuzzy Matching

**User Story:**
```gherkin
As a customer
I want to search for number plates using intelligent fuzzy matching
So that I can find plates even when I make typos or remember the registration incorrectly
```

**Additional Context:**
- UK number plates have different formats based on when they were issued:
  - Current (2001-present): AB51 ABC (region code, age identifier, random letters)
  - Prefix (1983-2001): A123 BCD 
  - Suffix (1963-1983): ABC 123D
  - Dateless: No year identifier
- Customers often misremember or mistype registrations (confuse O/0, I/1, 5/S, etc.)
- Search should handle common character substitutions and typos
- Implement similarity scoring to rank results by relevance
- Support phonetic matching for registrations that "sound similar"
- Users want to filter by plate format, price ranges, and fuzzy patterns
- Search should be performant even with large datasets (10,000+ plates)
- Results should be ranked by relevance/similarity score
- Think about caching strategies for frequently-used search patterns

**Time Estimate:** 3-4 hours

**Acceptance Criteria:**
```gherkin
Scenario: Fuzzy search with typos
  Given there is a plate "AB51 XYZ" in the system
  When I search for "AB51 XYS" (typo: S instead of Z)
  Then I should see "AB51 XYZ" as a high-confidence match
  And the similarity score should be displayed

Scenario: Handle common character confusion
  Given there is a plate "BO55 MAN" in the system
  When I search for "B055 MAN" (0 instead of O)
  Then I should see "BO55 MAN" as a match
  And the system should recognize O/0 substitution

Scenario: Phonetic matching
  Given there is a plate "PH11 LIP" in the system
  When I search for "FI11 LIP" (sounds similar)
  Then I should see "PH11 LIP" as a potential match
  And it should be ranked by phonetic similarity

Scenario: Filter by plate format with fuzzy search
  Given there are plates in multiple formats
  When I select "Current Format" and search for "AB5* XY*"
  Then I should see current format plates matching the fuzzy pattern
  And results should be ranked by similarity score

Scenario: Relevance ranking
  Given multiple plates partially match my search "AB5"
  When I view the results
  Then exact matches should appear first
  And partial matches should be ranked by similarity
  And each result should show a confidence/similarity percentage

Scenario: Performance with fuzzy matching on large dataset
  Given there are 10,000+ plates in the system
  When I perform a fuzzy search
  Then results should return within 2 seconds
  And the algorithm should efficiently prune low-confidence matches

Scenario: Combined filters with fuzzy search
  Given I want to find specific plates
  When I use fuzzy search "AB5* X*" with price range £2000-£10000
  Then I should see ranked results matching both criteria
  And the similarity score should be visible for each result
```

**Technical Requirements:**
- Implement fuzzy string matching algorithm (Levenshtein distance, Jaro-Winkler, or similar)
- Handle common character substitutions (O/0, I/1, S/5, B/8, Z/2, etc.)
- Implement phonetic matching (Soundex, Metaphone, or Double Metaphone)
- Add similarity scoring and result ranking system
- Extend existing search/filter functionality with fuzzy matching
- Add database indexes for query performance optimization
- Update API endpoints to support fuzzy search parameters
- Add intuitive search UI to **ONE frontend application** (your choice of MVC or Angular) showing:
  - Similarity scores
  - "Did you mean?" suggestions
  - Highlighted matching portions
- Implement intelligent result caching
- Add comprehensive unit tests for fuzzy matching algorithms
- Performance testing with large datasets

**What We're Looking For:**
- Understanding of string similarity algorithms and their trade-offs
- Efficient implementation that doesn't degrade performance
- Smart character substitution mapping
- Clean algorithm implementation with good separation of concerns
- Thoughtful UX showing confidence scores and alternative matches
- Performance optimization strategies (early pruning, caching, indexing)
- Testing strategy for fuzzy logic (edge cases, accuracy validation)
- Documentation of algorithm choices and similarity thresholds

---

## Challenge 3: Implement Audit Trail System

**User Story:**
```gherkin
As a business owner
I want to track all changes made to number plates
So that I can maintain compliance and investigate issues
```

**Additional Context:**
- Need to track all significant changes to plates for compliance and debugging
- Should capture state changes (status, price, reservations, sales)
- Audit logs must be queryable and exportable for compliance reports
- Must not significantly impact application performance
- Should integrate naturally with existing event bus architecture
- Consider data retention and archival strategies
- Think about who should have access to audit logs

**Time Estimate:** 3-4 hours

**Acceptance Criteria:**
```gherkin
Scenario: Track plate status change
  Given a plate is in "ForSale" status
  When an admin changes it to "Reserved"
  Then an audit entry should be created
  And it should contain the old status, new status, timestamp, and user ID

Scenario: Track price changes
  Given a plate has a price of £5000
  When the price is changed to £4500
  Then an audit entry should record the change
  And it should include the old price and new price

Scenario: Query audit history
  Given there are multiple audit entries for a plate
  When I view the audit history for that plate
  Then I should see all changes in chronological order
  And each entry should show what changed and who made the change

Scenario: Export audit logs
  Given I need to generate a compliance report
  When I request audit logs for a date range
  Then I should receive a downloadable CSV or JSON file
  And it should contain all audit entries for that period

Scenario: Performance impact
  Given the audit system is active
  When normal operations occur (reserve, unreserve, sell)
  Then the response time should not degrade by more than 5%
  And audit logging should not block the main transaction
```

**Technical Requirements:**
- Design and implement audit table/entity structure
- Implement audit capture mechanism (consider: decorators, events, interceptors, or other patterns)
- Create API endpoints for querying audit history
- Add UI for viewing audit trails in **ONE frontend** (your choice of MVC or Angular)
- Ensure async/non-blocking audit writes to prevent performance degradation
- Implement export functionality (CSV, JSON, or other formats)
- Consider storage growth and archival strategy
- Add comprehensive tests

**What We're Looking For:**
- Architectural thinking about where and how to capture audit events
- Performance considerations (async processing, batching, storage separation)
- Data modeling for efficient querying
- Clean abstractions and separation of concerns
- Scalability considerations
- Security and access control thinking
- Documentation of design decisions

---

## Evaluation Criteria

Regardless of which challenge you choose, we'll evaluate based on:

### Code Quality (40%)
- Clean, readable code
- Proper naming conventions
- Appropriate comments where needed
- Following C# and TypeScript best practices

### Architecture & Design (30%)
- Following existing patterns in the codebase
- Proper separation of concerns
- Good abstraction choices
- SOLID principles

### Functionality (20%)
- Meets acceptance criteria
- Handles edge cases
- Appropriate validation
- Good error handling

### Testing (10%)
- Unit tests for new functionality
- Tests are meaningful and clear
- Good coverage of happy path and edge cases

**Note:** We care more about quality than quantity. A well-implemented partial solution is better than a rushed complete solution with poor code quality.

## Getting Help

- Refer to the [README.md](README.md) for setup instructions
- Check the existing codebase for patterns to follow
- Use any resources you'd normally use (documentation, Stack Overflow, etc.)
- If you get completely stuck, document your approach and what you tried

## Submission Checklist

- [ ] Code compiles without errors
- [ ] Tests pass (run `dotnet test` and `npm test`)
- [ ] Added a brief explanation of your approach in a new `SOLUTION.md` file
- [ ] Committed your changes with clear commit messages
- [ ] Pushed to your forked repository
- [ ] Sent us the link to your fork

Good luck! We're excited to see your work.
