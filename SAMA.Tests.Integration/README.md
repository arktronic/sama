# Integration Tests

Integration tests that use a **real PostgreSQL database** with per-test-class schema isolation.

## How It Works

Each test class gets its own PostgreSQL schema (e.g., `test_checkintegrationtests_638775123456789`):
- Created in `[TestInitialize]`
- Migrations applied automatically
- Dropped in `[TestCleanup]`

This enables **parallel execution across test classes** while maintaining complete isolation.

## Configuration

Connection via environment variables (defaults match `appsettings.Development.json`):
- `POSTGRES_HOST` (default: `localhost`)
- `POSTGRES_PORT` (default: `5432`)
- `POSTGRES_DB` (default: `samadb`)
- `POSTGRES_USER` (default: `sama`)
- `POSTGRES_PASSWORD` (default: `sama-dev-pw`)

## Writing Tests

Inherit from `IntegrationTestBase` and use the provided `DbContext`:

```csharp
[TestClass]
public class MyIntegrationTests : IntegrationTestBase
{
    [TestMethod]
    public async Task ShouldDoSomething()
    {
        var entity = new MyEntity { Name = "Test" };
        DbContext.MyEntities.Add(entity);
        await DbContext.SaveChangesAsync();
        
        var result = await DbContext.MyEntities.FirstAsync();
        Assert.AreEqual("Test", result.Name);
    }
}
```

## Troubleshooting

### Clean Up Orphaned Schemas

If tests crash, schemas may not be dropped:

```sql
DO $$ 
DECLARE 
    schema_name text;
BEGIN
    FOR schema_name IN 
        SELECT s.schema_name 
        FROM information_schema.schemata s
        WHERE s.schema_name LIKE 'test_%'
    LOOP
        EXECUTE 'DROP SCHEMA IF EXISTS ' || quote_ident(schema_name) || ' CASCADE';
    END LOOP;
END $$;
