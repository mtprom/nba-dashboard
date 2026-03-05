using ApiTest.Tests;

// Pick which test to run:
//   RawApiTest          — raw JSON output for all 3 endpoints
//   ModelValidationTest — verifies deserialization of all 3 response models
//   BackfillDisplayTest — fetches 5 days of games, prints top scorers (~3 min)

await BackfillDisplayTest.RunAsync();
