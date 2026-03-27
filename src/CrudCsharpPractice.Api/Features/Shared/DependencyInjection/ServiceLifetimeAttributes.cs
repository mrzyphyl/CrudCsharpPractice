namespace CrudCsharpPractice.Api.Features.Shared.DependencyInjection;

[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public class ScopedAttribute : Attribute { }

[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public class TransientAttribute : Attribute { }

[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public class SingletonAttribute : Attribute { }