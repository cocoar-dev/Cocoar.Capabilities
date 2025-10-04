## Boolean Flag System Usage Examples

This document demonstrates the new boolean flag-based registry control system with method-level overrides.

### 1. Default Behavior (Enabled by Default)

```csharp
// Create context with default settings (both registries enabled by default)
var context = new CapabilityContext();

var document = new Document("example.txt");

// Auto-registration happens by default
var composer = context.For(document);
var composition = composer.Add(new MetadataCapability("author", "John")).Build();

// Both are automatically registered and accessible via API
context.Composers.TryGet(document, out var foundComposer);  // Returns true
context.Compositions.TryGet(document, out var foundComposition);  // Returns true
```

### 2. Explicitly Disable Registries

```csharp
// Create context with both registries disabled
var context = new CapabilityContext(new CapabilityContextOptions
{
    UseComposerRegistry = false,
    UseCompositionRegistry = false
});

var document = new Document("example.txt");

// No auto-registration happens
var composer = context.For(document);
var composition = composer.Add(new MetadataCapability("author", "John")).Build();

// Nothing is registered
context.Composers.TryGet(document, out var foundComposer);  // Returns false
context.Compositions.TryGet(document, out var foundComposition);  // Returns false
```

### 3. Method-Level Override (Enable for Specific Operations)

```csharp
// Create context with both registries disabled by default
var context = new CapabilityContext(new CapabilityContextOptions
{
    UseComposerRegistry = false,
    UseCompositionRegistry = false
});

var document = new Document("example.txt");

// Override the default behavior for this specific operation
var composer = context.For(document, useRegistry: true);
var composition = composer.Add(new MetadataCapability("author", "John")).Build(useRegistry: true);

// Both are now registered despite default settings
context.Composers.TryGet(document, out var foundComposer);  // Returns true
context.Compositions.TryGet(document, out var foundComposition);  // Returns true
```

### 4. Method-Level Override (Disable for Specific Operations)

```csharp
// Create context with both registries enabled by default
var context = new CapabilityContext(); // Default: UseComposerRegistry = true, UseCompositionRegistry = true

var document1 = new Document("example1.txt");
var document2 = new Document("example2.txt");

// Disable registration for specific operations
var composer1 = context.For(document1, useRegistry: false);
var composition1 = composer1.Add(new MetadataCapability("author", "John")).Build(useRegistry: false);

// Use default behavior (enabled)
var composer2 = context.For(document2);
var composition2 = composer2.Add(new MetadataCapability("author", "Jane")).Build();

// Only document2 is registered
context.Composers.TryGet(document1, out var foundComposer1);  // Returns false
context.Compositions.TryGet(document1, out var foundComposition1);  // Returns false

context.Composers.TryGet(document2, out var foundComposer2);  // Returns true
context.Compositions.TryGet(document2, out var foundComposition2);  // Returns true
```

### 5. Registry API with Overrides

```csharp
// Context with registries disabled by default
var context = new CapabilityContext(new CapabilityContextOptions
{
    UseComposerRegistry = false,
    UseCompositionRegistry = false
});

var document = new Document("example.txt");

// Use registry API with override to enable registration
var composer = context.Composers.GetOrCreate(document, useRegistry: true);
var composition = context.Compositions.GetOrCreateEmpty(document, useRegistry: true);

// Subsequent calls return the same instances
var sameComposer = context.Composers.GetOrCreate(document, useRegistry: true);
var sameComposition = context.Compositions.GetOrCreateEmpty(document, useRegistry: true);

Assert.Same(composer, sameComposer);
Assert.Same(composition, sameComposition);
```

### 6. Mixed Scenarios

```csharp
// Enable composer registry but disable composition registry
var context = new CapabilityContext(new CapabilityContextOptions
{
    UseComposerRegistry = true,
    UseCompositionRegistry = false
});

var document = new Document("example.txt");

// Only composer is auto-registered by default
var composer = context.For(document);
var composition = composer.Add(new MetadataCapability("author", "John")).Build();

context.Composers.TryGet(document, out var foundComposer);  // Returns true
context.Compositions.TryGet(document, out var foundComposition);  // Returns false

// Override composition registration for this specific operation
var anotherComposition = composer.Add(new MetadataCapability("editor", "Jane")).Build(useRegistry: true);

context.Compositions.TryGet(document, out var foundComposition2);  // Returns true now
```

### Key Benefits

1. **Explicit Control**: Boolean flags make registry behavior explicit and predictable
2. **Method-Level Granularity**: Per-operation control via method parameters
3. **Always Available**: Registries are always available, eliminating null checks
4. **Backward Compatibility**: Default settings maintain existing behavior
5. **Fine-Grained Control**: Mix and match settings per registry type and per operation
6. **Clear API**: `IsEnabledByDefault` property makes the configuration transparent