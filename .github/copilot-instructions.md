# CoffeeUpdateClient - GitHub Copilot Instructions

## Project Context
CoffeeUpdateClient is a WPF application that automatically updates World of Warcraft addons for players in the Coffee guild on US-Illidan. It features self-updating capabilities and uses a test-driven development approach with dependency injection and mocking.

**Technology Stack:**
- .NET/C# WPF Application
- ReactiveUI for MVVM data binding
- NUnit 3 for unit testing
- System.IO.Abstractions for file system operations
- Dependency injection with custom mock implementations

## Code Generation Guidelines

### 1. Test-Driven Development (TDD)
When generating new features:
```csharp
// ALWAYS generate corresponding unit tests
[Test]
public async Task MethodName_Scenario_ExpectedBehavior()
{
    var mockEnv = new MockEnv();
    var service = new ServiceClass(mockEnv);
    
    await service.SomeMethod();
    
    Assert.That(mockEnv.FileSystem.File.Exists("expected-file"), Is.True);
}
```

**Test Structure:** Organize tests with setup, execution, and verification sections separated by blank lines. Do NOT use //Arrange, //Act, //Assert comments.

### 2. File System Operations
**Required Pattern:**
```csharp
// ❌ NEVER use direct System.IO
// File.WriteAllText("path", content);

// ✅ ALWAYS use IFileSystem abstraction
private readonly IFileSystem _fileSystem;
_fileSystem.File.WriteAllText("path", content);
```

### 3. Service Singleton Pattern
**Initialization Pattern:**
```csharp
// Service must implement initialization
public async Task LoadConfigSingleton()
{
    // Initialize here
    _isInitialized = true;
}

public static ServiceClass Instance 
{
    get 
    {
        if (!_isInitialized)
            throw new InvalidOperationException("Call LoadConfigSingleton() first");
        return _instance;
    }
}
```

### 4. WPF/ReactiveUI Integration
**View Activation Pattern:**
```csharp
// Views should use WhenActivated for proper disposal
this.WhenActivated(disposableRegistration =>
{
    // All bindings go here and are disposed when view deactivates
});
```

**Data Binding Patterns:**
```csharp
// ❌ NEVER manipulate UI directly
// textBox.Text = "value";
// button.IsEnabled = condition;

// ✅ Property binding with transformation
this.WhenAnyValue(x => x.ViewModel.SomeProperty)
    .Select(value => TransformValue(value))
    .BindTo(this, view => view.TargetControl.Property)
    .DisposeWith(disposableRegistration);

// ✅ Command binding
this.BindCommand(ViewModel,
    vm => vm.SomeCommand,
    view => view.ButtonControl)
    .DisposeWith(disposableRegistration);

// ✅ One-way binding with converter
this.OneWayBind(ViewModel,
    vm => vm.StateProperty,
    view => view.Control.Property,
    value => ConvertValue(value))
    .DisposeWith(disposableRegistration);

// ✅ Complex binding with multiple sources
this.WhenAnyValue(x => x.ViewModel.Property1)
    .CombineLatest(this.WhenAnyValue(x => x.ViewModel.Property2))
    .Select(x => ProcessCombinedValues(x.First, x.Second))
    .BindTo(this, view => view.TargetControl.Property)
    .DisposeWith(disposableRegistration);
```

**ViewModel Properties:**
```csharp
// ✅ Use RaiseAndSetIfChanged for properties with backing fields
private string _propertyName;
public string PropertyName
{
    get => _propertyName;
    set => this.RaiseAndSetIfChanged(ref _propertyName, value);
}

// ✅ Use ObservableAsPropertyHelper for computed properties
private readonly ObservableAsPropertyHelper<string> _computedProperty;
public string ComputedProperty => _computedProperty.Value;

// ✅ Initialize computed properties in constructor with ToProperty
_computedProperty = this
    .WhenAnyValue(x => x.SourceProperty)
    .Select(value => TransformValue(value))
    .ToProperty(this, x => x.ComputedProperty);

// ✅ Use ReactiveCommand for commands with proper typing
public ReactiveCommand<Unit, string?> SomeCommand { get; }

// ✅ Initialize ReactiveCommand in constructor
SomeCommand = ReactiveCommand.Create<Unit, string?>(_ =>
{
    // Command implementation
    return result;
});

// ✅ Complex computed properties with multiple sources
_validationState = this
    .WhenAnyValue(x => x.Property1)
    .CombineLatest(this.WhenAnyValue(x => x.Property2))
    .Select(x =>
    {
        var (prop1, prop2) = x;
        return DetermineState(prop1, prop2);
    })
    .ToProperty(this, x => x.ValidationState);
```

### 5. NUnit 3 Test Patterns

**Async Testing:**
```csharp
[Test]
public async Task AsyncMethod_ValidInput_CompletesSuccessfully()
{
    // Use async/await, not .Result or .Wait()
    var result = await service.AsyncMethod();
    
    Assert.That(result, Is.Not.Null);
}
```

**Exception Testing:**
```csharp
[Test]
public void Method_InvalidState_ThrowsInvalidOperationException()
{
    Assert.Throws<InvalidOperationException>(() => 
    {
        var _ = service.Instance; // Before initialization
    });
}
```

**File Operation Verification:**
```csharp
[Test]
public async Task SaveConfig_ValidData_CreatesFile()
{
    var mockEnv = new MockEnv();
    var service = new FileSystemConfigService(mockEnv);
    
    await service.SaveConfig(configData);
    
    Assert.That(mockEnv.FileSystem.File.Exists(expectedPath), Is.True);
    var content = mockEnv.FileSystem.File.ReadAllText(expectedPath);
    Assert.That(content, Contains.Substring("expectedContent"));
}
```

## Project Structure & Naming Conventions

```
src/CoffeeUpdateClient/
├── Models/          # Data models and DTOs
├── Services/        # Business logic and external integrations
├── ViewModels/      # ReactiveUI ViewModels for data binding
└── Utils/           # Helper classes and extensions

tests/CoffeeUpdateClient.Tests/
├── Mocks/           # Mock implementations (MockEnv, etc.)
└── [Feature]Test.cs # Unit tests following FeatureNameTest pattern
```

## Code Quality Requirements

### Dependency Injection
```csharp
// Services should accept dependencies in constructor
public class AddonUpdateService
{
    private readonly IFileSystem _fileSystem;
    private readonly IHttpClient _httpClient;
    
    public AddonUpdateService(IEnvironment env)
    {
        _fileSystem = env.FileSystem;
        _httpClient = env.HttpClient;
    }
}
```

### Formatting
```bash
# Format a specific file
dotnet format --include src/CoffeeUpdateClient/Services/AddonUpdateService.cs
```
Always run `dotnet format` after making changes to any file, no matter how small. After running `dotnet format`, the file on disk will be updated to match the formatting rules; include the changes in the changeset presented to the user. Do not attempt to further format the file after running the tool.

### Error Handling
```csharp
// Provide meaningful error messages
throw new InvalidOperationException(
    $"Cannot access {nameof(Instance)} before calling {nameof(LoadConfigSingleton)}");

// Test error conditions
[Test]
public void Method_NullInput_ThrowsArgumentNullException()
{
    var ex = Assert.Throws<ArgumentNullException>(() => service.Method(null));
    
    Assert.That(ex.ParamName, Is.EqualTo("expectedParamName"));
}
```

### Async Best Practices
```csharp
// Use ConfigureAwait(false) in library code
await SomeAsyncOperation().ConfigureAwait(false);

// Prefer Task over async void (except event handlers)
public async Task ProcessAsync() { /* ... */ }
```

## Build and Test Commands

### Building the Project
```bash
dotnet build
```
Use this command to compile the entire solution and verify that all code compiles successfully.

### Running Tests
```bash
dotnet test
```
Use this command to run all unit tests in the test projects. Always run tests after making changes to ensure no regressions are introduced.

## Mandatory Rules
1. **All new features require unit tests** - No exceptions
2. **Use IFileSystem abstraction** - Never direct System.IO calls
3. **Initialize singletons before use** - Throw InvalidOperationException if not initialized
4. **Follow ReactiveUI patterns** - No manual UI state management
5. **Use NUnit 3 syntax** - No legacy NUnit 2 patterns
6. **Verify file operations in tests** - Assert file existence, content, and deletion
7. **Run existing tests after changes** - Ensure no regressions
8. **Format all modified files** - Run `dotnet format` after any code changes

## Common Anti-Patterns to Avoid
```csharp
// ❌ Don't use legacy patterns
Assert.AreEqual(expected, actual);           // Use Assert.That() instead
File.ReadAllText(path);                      // Use IFileSystem instead
PropertyChanged?.Invoke(this, new...);       // Use ReactiveUI instead
await task.Result;                           // Use await directly instead
```

When generating code, prioritize testability, maintainability, and adherence to the established patterns in this codebase.
