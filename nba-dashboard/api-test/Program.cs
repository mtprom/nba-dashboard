using ApiTest.Tests;

// Pick which test to run:
//   RawApiTest              — raw JSON output for all 3 endpoints
//   ModelValidationTest     — verifies deserialization of all 3 response models
//   BackfillDisplayTest     — fetches 5 days of games, prints top scorers (~3 min)
//   SeasonStatsApiTest      — validates leaguedashplayerstats (traditional + advanced)
//   CurlResultTest          — unit tests for block detection heuristics (no network)
//   RotatedHeaderApiTest    — integration test: tries each header profile against NBA API

await CurlResultTest.RunAsync();
