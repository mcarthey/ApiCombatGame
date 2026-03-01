# Lesson Plan: Data Analysis with APIs

**Duration:** 3 weeks (15 class periods, 50 minutes each)
**Level:** Intermediate (assumes basic programming; no prior API experience required)
**Platform:** [API Combat](https://apicombat.com) — "The API is the game"
**Subject Integration:** Computer Science + Statistics/Data Science

---

## Course Overview

Students learn to collect, analyze, and visualize data from a live REST API. Using API Combat's battle simulation system, they design experiments, gather statistically significant samples, calculate metrics, and draw evidence-based conclusions — the scientific method applied to API data.

---

## Wisconsin CS Standards Alignment

| Standard | Code | How This Curriculum Addresses It |
|----------|------|----------------------------------|
| Computational data collection | DA3.a.6.h | Batch practice → structured battle data → analysis |
| Storage & retrieval | DA2.a.4.h | Battle history, replay data, JSON parsing |
| Design algorithmic solutions | AP1.a.8.h | Strategy optimization through data-driven iteration |
| Develop test cases | AP1.a.14.h | Hypothesis testing with controlled battle experiments |
| Decompose problems | AP2.a.13.h | Breaking analysis into data collection → cleaning → visualization → conclusions |
| Use API documentation | AP3.c.5.h | Reading OpenAPI spec to understand data schemas |
| Ethical collaboration | IC2.c.5.h | Sharing findings, peer review of analysis |

---

## Week 1: Data Collection (5 periods)

### Learning Objectives
- Make authenticated API calls to collect structured data
- Use batch endpoints for efficient large-scale data collection
- Parse JSON responses and extract relevant fields
- Store collected data in a structured format (CSV, JSON, or database)

### Day 1: Your First Data Source

**Setup (20 min):**
1. Register and log in to API Combat
2. Explore your roster and configure a team
3. Run your first AI practice battle

**Hands-on (30 min):**
1. Fight 3 AI opponents (novice tier) and collect the results:
   ```bash
   # List AI opponents
   curl https://apicombat.com/api/v1/ai/opponents \
     -H "Authorization: Bearer $TOKEN"

   # Fight one
   curl -X POST https://apicombat.com/api/v1/ai/practice \
     -H "Authorization: Bearer $TOKEN" \
     -H "Content-Type: application/json" \
     -d '{"teamId": "YOUR_TEAM_ID", "opponentId": "novice-1"}'
   ```
2. Examine the JSON response — what data fields are returned?
3. Identify: What could you measure? What questions could this data answer?

**Deliverable:** List of 5 questions you could answer with battle data

### Day 2: Batch Data Collection

**Hands-on (40 min):**
1. Use the batch practice endpoint to generate large datasets efficiently:
   ```bash
   curl -X POST https://apicombat.com/api/v1/ai/batch-practice \
     -H "Authorization: Bearer $TOKEN" \
     -H "Content-Type: application/json" \
     -d '{
       "teamId": "YOUR_TEAM_ID",
       "opponentId": "novice-1",
       "count": 50
     }'
   ```
   Response:
   ```json
   {
     "totalBattles": 50,
     "wins": 34,
     "losses": 16,
     "winRate": 68.0,
     "avgTurns": 7.2,
     "opponentName": "Training Dummy"
   }
   ```
2. Run batch practice against all 3 novice opponents (50 battles each)
3. Record results in a spreadsheet or CSV file

**Discussion (10 min):** Is 50 battles enough to be confident in the win rate? How would you know?

**Deliverable:** CSV/spreadsheet with batch results for 3 opponents

### Day 3: Sample Size & Confidence

**Hands-on (35 min):**

Run the same matchup (your team vs novice-1) at different sample sizes:

| Sample Size | Win Rate | Observation |
|-------------|----------|-------------|
| 10 | | |
| 25 | | |
| 50 | | |
| 100 | | |
| 200 | | |

```bash
# Run with count=10, then 25, then 50, etc.
curl -X POST https://apicombat.com/api/v1/ai/batch-practice \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"teamId": "YOUR_TEAM_ID", "opponentId": "novice-1", "count": 10}'
```

**Discussion (15 min):**
- Do the win rates converge as sample size increases?
- How much does the 10-battle win rate vary between runs?
- What sample size gives you confidence the result is "real"?

**Deliverable:** Completed table with analysis paragraph on convergence

### Day 4: Battle History Analysis

**Hands-on (40 min):**
1. Retrieve your battle history:
   ```bash
   curl https://apicombat.com/api/v1/battle/history \
     -H "Authorization: Bearer $TOKEN"
   ```
2. Write a script to parse the JSON and extract:
   - Win/loss record
   - Average battle duration (turns)
   - Win rate over time (is it improving?)
3. Store results in a structured format

**Discussion (10 min):** What's the difference between batch practice data (aggregate) and battle history (individual records)? When would you use each?

**Deliverable:** Script that parses battle history JSON and outputs summary statistics

### Day 5: Data Collection Assessment

**Practical lab (50 min):**

Write a data collection script that:
1. Authenticates with the API
2. Runs batch practice against 3 different difficulty tiers (novice, intermediate, expert)
3. Collects win rate and average turns for each
4. Saves results to a CSV file with columns: opponent, tier, battles, wins, losses, winRate, avgTurns
5. Handles rate limiting gracefully

**Grading:** Script runs, collects correct data, handles errors, produces clean CSV

---

## Week 2: Analysis & Experimentation (5 periods)

### Learning Objectives
- Design controlled experiments with independent and dependent variables
- Calculate descriptive statistics from API data
- Test hypotheses using battle data
- Create data visualizations (charts/graphs)

### Day 6: Experimental Design

**Discussion (20 min):** The scientific method applied to games:
- **Hypothesis:** "Formation A wins more than Formation B against intermediate opponents"
- **Independent variable:** Team formation/strategy
- **Dependent variable:** Win rate
- **Control:** Same opponent, same sample size
- **Sample size:** 100 battles per configuration (batch practice)

**Hands-on (30 min):**
1. Configure two different teams (different unit compositions or positions)
2. State your hypothesis
3. Run 100 batch practice battles with each team against the same opponent
4. Record results

**Deliverable:** Hypothesis statement + experimental design document

### Day 7: Running Your Experiment

**Hands-on (50 min):**

Full lab period — execute your experiment:
1. Run batch practice for Team A: 100 battles vs intermediate-1
2. Run batch practice for Team B: 100 battles vs intermediate-1
3. Repeat each run 3 times (for variance measurement)
4. Record all results

```bash
# Team A
curl -X POST https://apicombat.com/api/v1/ai/batch-practice \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"teamId": "TEAM_A_ID", "opponentId": "intermediate-1", "count": 100}'

# Team B (configure a different team first)
curl -X POST https://apicombat.com/api/v1/ai/batch-practice \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"teamId": "TEAM_B_ID", "opponentId": "intermediate-1", "count": 100}'
```

**Deliverable:** Raw data table with all 6 runs (3 per team)

### Day 8: Descriptive Statistics

**Hands-on (40 min):**

Using your experimental data, calculate:
- Mean win rate for each team
- Range (highest run - lowest run)
- Is there overlap in the ranges? What does that tell you?

Create visualizations (use your preferred tool — Excel, Google Sheets, Python matplotlib, etc.):
1. Bar chart: Win rate comparison (Team A vs Team B)
2. Line chart: Win rate across the 3 runs for each team (consistency)
3. Bar chart: Average turns per battle (which team wins/loses faster?)

**Discussion (10 min):** If Team A wins 67% and Team B wins 63%, is Team A actually better? How would you know for sure?

**Deliverable:** Calculations + 3 visualizations with labels and titles

### Day 9: Cross-Difficulty Analysis

**Hands-on (40 min):**

Take your best team and test it across all difficulty tiers:

```bash
# Run against each tier
for opponent in novice-1 novice-2 novice-3 intermediate-1 intermediate-2 intermediate-3 expert-1 expert-2 expert-3; do
  curl -X POST https://apicombat.com/api/v1/ai/batch-practice \
    -H "Authorization: Bearer $TOKEN" \
    -H "Content-Type: application/json" \
    -d "{\"teamId\": \"$TEAM_ID\", \"opponentId\": \"$opponent\", \"count\": 100}"
done
```

Create a visualization showing win rate vs opponent difficulty.

**Discussion (10 min):** Is the difficulty curve linear? Does your team have a "ceiling" where it stops winning? What could you change to break through?

**Deliverable:** Cross-difficulty data table + visualization + analysis paragraph

### Day 10: Mid-Course Assessment

**Written component (20 min):**
1. Define: independent variable, dependent variable, control, sample size
2. Why do we run experiments multiple times instead of once?
3. What's the minimum sample size you'd trust for a win rate claim? Justify.

**Practical component (30 min):**
Design (on paper) an experiment to answer: "Does unit composition matter more than team strategy against expert opponents?" Include:
- Hypothesis
- Variables (independent, dependent, controlled)
- Data collection plan (which endpoints, how many battles)
- How you'd analyze the results

---

## Week 3: Advanced Analysis & Presentation (5 periods)

### Learning Objectives
- Combine multiple data sources (battle history + leaderboard + batch practice)
- Perform comparative analysis across the class
- Create a professional data analysis report
- Present findings with supporting evidence

### Day 11: Leaderboard Data Analysis

**Hands-on (35 min):**
1. Fetch the class leaderboard:
   ```bash
   curl https://apicombat.com/api/v1/education/modules/{moduleId}/leaderboard \
     -H "Authorization: Bearer $TOKEN"
   ```
2. Fetch the global leaderboard for comparison:
   ```bash
   curl https://apicombat.com/api/v1/leaderboard?limit=25 \
     -H "Authorization: Bearer $TOKEN"
   ```
3. Analyze: How does the class distribution compare to the global distribution?
4. Visualize: Histogram of class ratings vs global top 25

**Discussion (15 min):** Selection bias — the global leaderboard shows top players, not all players. How does this affect your comparison?

**Deliverable:** Comparative visualization with written interpretation

### Day 12: Strategy Marketplace Data

**Hands-on (40 min):**
1. Browse the strategy marketplace:
   ```bash
   curl https://apicombat.com/api/v1/strategies/browse \
     -H "Authorization: Bearer $TOKEN"
   ```
2. Identify top-rated strategies — what makes them popular?
3. Download a community strategy and test it via batch practice (100 battles)
4. Compare its win rate to your own best strategy

**Discussion (10 min):** Marketplace dynamics — why do people share strategies? Does sharing reduce competitive advantage?

**Deliverable:** Comparison table: your strategy vs community strategy with batch practice data

### Day 13: Report Writing Lab

**Lab (50 min):**

Write your final analysis report. Required sections:
1. **Introduction:** What question did you investigate?
2. **Method:** How did you collect data? (endpoints used, sample sizes, controls)
3. **Results:** Summary statistics, visualizations, key findings
4. **Analysis:** What do the results mean? Were your hypotheses supported?
5. **Limitations:** Sample size, variables not controlled, potential biases
6. **Conclusion:** One-paragraph summary of findings

Minimum requirements:
- 3 data visualizations (charts/graphs)
- 200+ total battles as data source
- At least one comparison (Team A vs B, tier vs tier, or strategy vs strategy)

### Day 14: Peer Review

**Activity (50 min):**
1. Exchange reports with a partner
2. Review using this checklist:
   - [ ] Hypothesis is clearly stated
   - [ ] Methods section describes data collection precisely
   - [ ] Visualizations have titles, axis labels, and legends
   - [ ] Conclusions are supported by the data (no unsupported claims)
   - [ ] Limitations are acknowledged
   - [ ] Sample sizes are adequate
3. Provide written feedback (strengths + 2 suggestions)
4. Revise your report based on feedback

**Deliverable:** Peer review feedback given + received, revised report

### Day 15: Presentations & Class Tournament

**Presentations (30 min):**
Each student gives a 3-minute presentation:
- Key finding (one sentence)
- Most interesting visualization
- What surprised you

**Class Tournament (20 min):**
Instructor launches a class tournament to cap the course:
```bash
curl -X POST https://apicombat.com/api/v1/education/modules/{moduleId}/tournament \
  -H "Authorization: Bearer $INSTRUCTOR_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"entryFee": 0, "maxParticipants": 32}'
```
Students register and see who optimized their team best — data analysis meets competition.

---

## Materials & Resources

- **API Documentation:** https://apicombat.com/api-docs/v1
- **OpenAPI Spec:** https://apicombat.com/openapi/v1.json
- **Education Mode Guide:** See EDUCATION_MODE.md for module setup and enrollment
- **Batch Practice:** Up to 200 battles per API call — ideal for data collection
- **Recommended tools:** Python (pandas + matplotlib), Google Sheets, or Excel for analysis

## Assessment Summary

| Component | Weight | Description |
|-----------|--------|-------------|
| Data collection scripts | 20% | Working scripts that gather and store battle data |
| Experimental design | 15% | Clear hypothesis, variables, controls, sample sizes |
| Visualizations | 20% | 3+ charts with proper labels, titles, and correct chart types |
| Final report | 30% | Complete analysis following the required structure |
| Peer review & presentation | 15% | Constructive feedback + clear 3-minute presentation |

## Curriculum Module Setup

```bash
curl -X POST https://apicombat.com/api/v1/education/modules \
  -H "Authorization: Bearer $INSTRUCTOR_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "title": "Data Analysis with APIs",
    "description": "Collect, analyze, and visualize battle data using REST APIs. Design experiments, test hypotheses, and present findings.",
    "difficulty": "intermediate",
    "lessons": [
      {
        "title": "First Data Collection",
        "objective": "Run AI practice battles and examine the JSON response structure.",
        "endpoint": "POST /api/v1/ai/practice",
        "hint": "List opponents with GET /api/v1/ai/opponents first."
      },
      {
        "title": "Batch Data Collection",
        "objective": "Use batch practice to generate 50+ battle results in a single API call.",
        "endpoint": "POST /api/v1/ai/batch-practice",
        "hint": "Set count to 50 or higher. Save the response for analysis."
      },
      {
        "title": "Sample Size Experiment",
        "objective": "Run the same matchup at 5 different sample sizes and observe convergence.",
        "endpoint": "POST /api/v1/ai/batch-practice",
        "hint": "Try count=10, 25, 50, 100, 200 against the same opponent."
      },
      {
        "title": "Battle History Analysis",
        "objective": "Retrieve your battle history and calculate summary statistics from individual records.",
        "endpoint": "GET /api/v1/battle/history",
        "hint": "Parse the JSON array and compute win rate, average turns, and trends."
      },
      {
        "title": "A/B Strategy Test",
        "objective": "Run 100 battles each with two different team configurations and compare win rates.",
        "endpoint": "POST /api/v1/ai/batch-practice",
        "hint": "Configure two teams, run 100 battles each vs the same opponent."
      },
      {
        "title": "Class Leaderboard Analysis",
        "objective": "Fetch the class leaderboard and compare class performance to the global leaderboard.",
        "endpoint": "GET /api/v1/education/modules/{moduleId}/leaderboard",
        "hint": "Also fetch GET /api/v1/leaderboard for global comparison data."
      }
    ]
  }'
```
