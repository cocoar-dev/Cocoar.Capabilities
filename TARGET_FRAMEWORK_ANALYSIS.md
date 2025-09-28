# Target Framework Analysis for Cocoar.Capabilities

## Current State
- **Current Target**: `net9.0`
- **Libraries**: Split into `Cocoar.Capabilities.Core` and `Cocoar.Capabilities`

## Language Feature Analysis

### **🔴 BLOCKING: C# 9.0+ Features (Require .NET 5+)**
1. **Records with primary constructors** (C# 9.0)
   - `public record TestCapability(string Value) : ICapability<TestSubject>;`
   - `public record PrimaryTestCapability(string Value) : ...`
   - Used extensively in test helpers and throughout the codebase

2. **Nullable reference types with `?`** (C# 8.0, but heavily used)
   - `public IPrimaryCapability<TSubject>? GetPrimaryOrDefault()`
   - Used for optional return types

### **🟡 LIMITING: .NET Framework API Requirements**

1. **ConditionalWeakTable<TKey, TValue>** (.NET Framework 4.0+)
   - Used in `CompositionRegistry.cs`
   - Available since .NET Framework 4.0, .NET Core 1.0, .NET 5+

2. **ValueTuple support** (.NET Framework 4.7+, .NET Core 1.0+)
   - Core feature for multi-contract registration
   - `AddAs<(IValidationCapability, IEmailCapability)>(capability)`
   - System.ValueTuple package available for older frameworks

3. **ArgumentNullException.ThrowIfNull()** (.NET 6+)
   - Used throughout the codebase
   - Can be easily replaced with manual null checks

### **✅ COMPATIBLE: Standard Features**
- Generics, interfaces, collections
- LINQ (used minimally)
- Standard .NET reflection APIs
- Exception handling patterns

## **Recommendation Analysis**

### **Option 1: Keep .NET 5+ (net5.0+) - RECOMMENDED** 
**Pros:**
- ✅ Keep all records (major syntax benefit)
- ✅ Native nullable reference types
- ✅ Modern C# syntax
- ✅ Broad compatibility (supports .NET 5, 6, 7, 8, 9)
- ✅ Future-proof

**Cons:**
- ❌ No .NET Framework support
- ❌ No .NET Core 2.x/3.x support

### **Option 2: Target .NET Standard 2.0 + .NET 6** 
**Pros:**
- ✅ Broader compatibility (.NET Framework 4.6.1+)
- ✅ Keep modern syntax

**Cons:**
- ❌ Would need to replace records with classes/structs
- ❌ Significant code changes required
- ❌ Loss of modern C# benefits

### **Option 3: Multi-target (netstandard2.0;net5.0;net6.0;net8.0)**
**Pros:**
- ✅ Maximum compatibility
- ✅ Optimal for each platform

**Cons:**
- ❌ Complex build configuration
- ❌ Would need conditional compilation
- ❌ Code complexity increase

## **🚀 AMAZING: You can target .NET Standard 2.0!**

### **Why .NET Standard 2.0 is PERFECT:**

1. **Library code uses NO records** - records are only in test code!
2. **Maximum compatibility**: .NET Framework 4.6.1+, .NET Core 2.0+, .NET 5+
3. **Tests can stay modern** - test projects can target net5.0+ independently
4. **Enterprise gold standard** - .NET Standard 2.0 is the sweet spot
5. **Future-proof** - works everywhere

### **Required Changes for netstandard2.0:**
```csharp
// Replace this (.NET 6+ only):
ArgumentNullException.ThrowIfNull(subject);

// With this (.NET Standard 2.0 compatible):
if (subject is null) throw new ArgumentNullException(nameof(subject));
```

### **Project Structure:**
```xml
<!-- Library projects -->
<TargetFramework>netstandard2.0</TargetFramework>

<!-- Test projects -->
<TargetFramework>net5.0</TargetFramework> <!-- Keep modern features for tests! -->
```

### **Market Analysis:**
- **.NET 5+**: Modern, actively supported, enterprise-ready
- **.NET Framework**: Legacy, maintenance mode
- **.NET Core 3.1**: EOL December 2022
- **Enterprise Reality**: Most modern projects target .NET 5+

### **Package Ecosystem:**
Modern NuGet packages increasingly target .NET 5+ as baseline, making this choice future-compatible.

## **Action Plan:**
1. ✅ Change target from `net9.0` → `net5.0`
2. ✅ Replace `ArgumentNullException.ThrowIfNull()` calls
3. ✅ Test compatibility 
4. ✅ Update packaging metadata

This gives you **maximum compatibility without sacrificing modern language features**.