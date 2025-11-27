# Run Tests

Run al of these and fix any error.

## Quick Tests (non-interactive)
```bash
cd Source/Test/bin/Debug/net472

# Narration test (Mid Game + Small Raid)
echo -e "2\n2\n1\n0" | ./NarratorTest.exe

# Choice test (Mid Game)
echo -e "3\n2\n0" | ./NarratorTest.exe
```

## Unit Tests
```bash
cd Source/Test && dotnet test NarratorUnitTests.csproj
```
