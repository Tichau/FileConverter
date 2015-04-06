/*
 * Copyright 2007-2012 JetBrains s.r.o.
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 * http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using System;

namespace FileConverter.Annotations
{
  /// <summary>
  /// Indicates that marked element should be localized or not.
  /// </summary>
  /// <example>
  /// <code>
  /// [LocalizationRequiredAttribute(true)]
  /// public class Foo
  /// {
  ///   private string str = "my string"; // Warning: Localizable string
  /// }
  /// </code>
  /// </example>
  [AttributeUsage(AttributeTargets.All, AllowMultiple = false, Inherited = true)]
  public sealed class LocalizationRequiredAttribute : Attribute
  {
    /// <summary>
    /// Initializes a new instance of the <see cref="LocalizationRequiredAttribute"/> class with
    /// <see cref="Required"/> set to <see langword="true"/>.
    /// </summary>
    public LocalizationRequiredAttribute() : this(true)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="LocalizationRequiredAttribute"/> class.
    /// </summary>
    /// <param name="required"><c>true</c> if a element should be localized; otherwise, <c>false</c>.</param>
    public LocalizationRequiredAttribute(bool required)
    {
      Required = required;
    }

    /// <summary>
    /// Gets a value indicating whether a element should be localized.
    /// <value><c>true</c> if a element should be localized; otherwise, <c>false</c>.</value>
    /// </summary>
    [UsedImplicitly] public bool Required { get; private set; }

    /// <summary>
    /// Returns whether the value of the given object is equal to the current <see cref="LocalizationRequiredAttribute"/>.
    /// </summary>
    /// <param name="obj">The object to test the value equality of. </param>
    /// <returns>
    /// <c>true</c> if the value of the given object is equal to that of the current; otherwise, <c>false</c>.
    /// </returns>
    public override bool Equals(object obj)
    {
      var attribute = obj as LocalizationRequiredAttribute;
      return attribute != null && attribute.Required == Required;
    }

    /// <summary>
    /// Returns the hash code for this instance.
    /// </summary>
    /// <returns>A hash code for the current <see cref="LocalizationRequiredAttribute"/>.</returns>
    public override int GetHashCode()
    {
      return base.GetHashCode();
    }
  }

  /// <summary>
  /// Indicates that the marked method builds string by format pattern and (optional) arguments. 
  /// Parameter, which contains format string, should be given in constructor.
  /// The format string should be in <see cref="string.Format(IFormatProvider,string,object[])"/> -like form
  /// </summary>
  /// <example>
  /// <code>
  /// [StringFormatMethod("message")]
  /// public void ShowError(string message, params object[] args)
  /// {
  ///   //Do something
  /// }
  /// public void Foo()
  /// {
  ///   ShowError("Failed: {0}"); // Warning: Non-existing argument in format string
  /// }
  /// </code>
  /// </example>
  [AttributeUsage(AttributeTargets.Constructor | AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
  public sealed class StringFormatMethodAttribute : Attribute
  {
    /// <summary>
    /// Initializes new instance of StringFormatMethodAttribute
    /// </summary>
    /// <param name="formatParameterName">Specifies which parameter of an annotated method should be treated as format-string</param>
    public StringFormatMethodAttribute(string formatParameterName)
    {
      FormatParameterName = formatParameterName;
    }

    /// <summary>
    /// Gets format parameter name
    /// </summary>
    [UsedImplicitly] public string FormatParameterName { get; private set; }
  }

  /// <summary>
  /// Indicates that the function argument should be string literal and match one of the parameters
  /// of the caller function.
  /// For example, ReSharper annotates the parameter of <see cref="System.ArgumentNullException"/>.
  /// </summary>
  /// <example>
  /// <code>
  /// public void Foo(string param)
  /// {
  ///   if (param == null)
  ///     throw new ArgumentNullException("par"); // Warning: Cannot resolve symbol
  /// }
  /// </code>
  /// </example>
  [AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false, Inherited = true)]
  public sealed class InvokerParameterNameAttribute : Attribute { }
  
  /// <summary>
  /// Indicates that the method is contained in a type that implements
  /// <see cref="System.ComponentModel.INotifyPropertyChanged"/> interface
  /// and this method is used to notify that some property value changed.
  /// </summary>
  /// <remarks>
  /// The method should be non-static and conform to one of the supported signatures:
  /// <list>
  /// <item><c>NotifyChanged(string)</c></item>
  /// <item><c>NotifyChanged(params string[])</c></item>
  /// <item><c>NotifyChanged{T}(Expression{Func{T}})</c></item>
  /// <item><c>NotifyChanged{T,U}(Expression{Func{T,U}})</c></item>
  /// <item><c>SetProperty{T}(ref T, T, string)</c></item>
  /// </list>
  /// </remarks>
  /// <example>
  /// <code>
  /// public class Foo : INotifyPropertyChanged
  /// {
  ///   public event PropertyChangedEventHandler PropertyChanged;
  ///
  ///   [NotifyPropertyChangedInvocator]
  ///   protected virtual void NotifyChanged(string propertyName)
  ///   {}
  ///
  ///   private string _name;
  ///   public string Name
  ///   {
  ///     get { return _name; }
  ///     set
  ///     {
  ///       _name = value;
  ///       NotifyChanged("LastName"); // Warning
  ///     }
  ///   }
  /// }
  /// </code>
  /// Examples of generated notifications:
  /// <list>
  /// <item><c>NotifyChanged("Property")</c></item>
  /// <item><c>NotifyChanged(() => Property)</c></item>
  /// <item><c>NotifyChanged((VM x) => x.Property)</c></item>
  /// <item><c>SetProperty(ref myField, value, "Property")</c></item>
  /// </list>
  /// </example>
  [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
  public sealed class NotifyPropertyChangedInvocatorAttribute : Attribute
  {
    public NotifyPropertyChangedInvocatorAttribute() { }
    public NotifyPropertyChangedInvocatorAttribute(string parameterName)
    {
      ParameterName = parameterName;
    }

    [UsedImplicitly] public string ParameterName { get; private set; }
  }

  /// <summary>
  /// Indicates that the value of the marked element could be <c>null</c> sometimes, 
  /// so the check for <c>null</c> is necessary before its usage.
  /// </summary>
  /// <example>
  /// <code>
  /// [CanBeNull]
  /// public object Test()
  /// {
  ///   return null;
  /// }
  /// 
  /// public void UseTest()
  /// {
  ///   var p = Test(); 
  ///   var s = p.ToString(); // Warning: Possible 'System.NullReferenceException' 
  /// }
  /// </code>
  /// </example>
  [AttributeUsage(AttributeTargets.Method | AttributeTargets.Parameter | AttributeTargets.Property | AttributeTargets.Delegate | AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
  public sealed class CanBeNullAttribute : Attribute { }

  /// <summary>
  /// Indicates that the value of the marked element could never be <c>null</c>
  /// </summary>
  /// <example>
  /// <code>
  /// [NotNull]
  /// public object Foo()
  /// {
  ///   return null; // Warning: Possible 'null' assignment
  /// } 
  /// </code>
  /// </example>
  [AttributeUsage(AttributeTargets.Method | AttributeTargets.Parameter | AttributeTargets.Property | AttributeTargets.Delegate | AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
  public sealed class NotNullAttribute : Attribute { }

  /// <summary>
  /// Describes dependency between method input and output.
  /// </summary>
  /// <syntax>
  /// <p>Function Definition Table syntax:</p>
  /// <list>
  /// <item>FDT      ::= FDTRow [;FDTRow]*</item>
  /// <item>FDTRow   ::= Input =&gt; Output | Output &lt;= Input</item>
  /// <item>Input    ::= ParameterName: Value [, Input]*</item>
  /// <item>Output   ::= [ParameterName: Value]* {halt|stop|void|nothing|Value}</item>
  /// <item>Value    ::= true | false | null | notnull | canbenull</item>
  /// </list>
  /// If method has single input parameter, it's name could be omitted. <br/>
  /// Using <c>halt</c> (or <c>void</c>/<c>nothing</c>, which is the same) for method output means that the methos doesn't return normally. <br/>
  /// <c>canbenull</c> annotation is only applicable for output parameters. <br/>
  /// You can use multiple <c>[ContractAnnotation]</c> for each FDT row, or use single attribute with rows separated by semicolon. <br/>
  /// </syntax>
  /// <examples>
  /// <list>
  /// <item><code>
  /// [ContractAnnotation("=> halt")]
  /// public void TerminationMethod()
  /// </code></item>
  /// <item><code>
  /// [ContractAnnotation("halt &lt;= condition: false")]
  /// public void Assert(bool condition, string text) // Regular Assertion method
  /// </code></item>
  /// <item><code>
  /// [ContractAnnotation("s:null => true")]
  /// public bool IsNullOrEmpty(string s) // String.IsNullOrEmpty
  /// </code></item>
  /// <item><code>
  /// // A method that returns null if the parameter is null, and not null if the parameter is not null
  /// [ContractAnnotation("null => null; notnull => notnull")]
  /// public object Transform(object data) 
  /// </code></item>
  /// <item><code>
  /// [ContractAnnotation("s:null=>false; =>true,result:notnull; =>false, result:null")]
  /// public bool TryParse(string s, out Person result)
  /// </code></item>
  /// </list>
  /// </examples>
  [AttributeUsage(AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
  public sealed class ContractAnnotationAttribute : Attribute
  {
    public ContractAnnotationAttribute([NotNull] string fdt) : this (fdt, false)
    {
    }

    public ContractAnnotationAttribute([NotNull] string fdt, bool forceFullStates)
    {
      FDT = fdt;
      ForceFullStates = forceFullStates;
    }

    public string FDT { get; private set; }
    public bool ForceFullStates { get; private set; }
  }

  /// <summary>
  /// Indicates that the value of the marked type (or its derivatives)
  /// cannot be compared using '==' or '!=' operators and <c>Equals()</c> should be used instead.
  /// However, using '==' or '!=' for comparison with <c>null</c> is always permitted.
  /// </summary>
  /// <example>
  /// <code>
  /// [CannotApplyEqualityOperator]
  /// class NoEquality
  /// {
  /// }
  /// 
  /// class UsesNoEquality
  /// {
  ///   public void Test()
  ///   {
  ///     var ca1 = new NoEquality();
  ///     var ca2 = new NoEquality();
  /// 
  ///     if (ca1 != null) // OK
  ///     {
  ///       bool condition = ca1 == ca2; // Warning
  ///     }
  ///   }
  /// }
  /// </code>
  /// </example>
  [AttributeUsage(AttributeTargets.Interface | AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = false, Inherited = true)]
  public sealed class CannotApplyEqualityOperatorAttribute : Attribute { }

  /// <summary>
  /// When applied to a target attribute, specifies a requirement for any type marked with 
  /// the target attribute to implement or inherit specific type or types.
  /// </summary>
  /// <example>
  /// <code>
  /// [BaseTypeRequired(typeof(IComponent)] // Specify requirement
  /// public class ComponentAttribute : Attribute 
  /// {}
  /// 
  /// [Component] // ComponentAttribute requires implementing IComponent interface
  /// public class MyComponent : IComponent
  /// {}
  /// </code>
  /// </example>
  [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = true)]
  [BaseTypeRequired(typeof(Attribute))]
  public sealed class BaseTypeRequiredAttribute : Attribute
  {
    /// <summary>
    /// Initializes new instance of BaseTypeRequiredAttribute
    /// </summary>
    /// <param name="baseType">Specifies which types are required</param>
    public BaseTypeRequiredAttribute(Type baseType)
    {
      BaseTypes = new[] { baseType };
    }

    /// <summary>
    /// Gets enumerations of specified base types
    /// </summary>
    public Type[] BaseTypes { get; private set; }
  }

  /// <summary>
  /// Indicates that the marked symbol is used implicitly (e.g. via reflection, in external library),
  /// so this symbol will not be marked as unused (as well as by other usage inspections)
  /// </summary>
  [AttributeUsage(AttributeTargets.All, AllowMultiple = false, Inherited = true)]
  public sealed class UsedImplicitlyAttribute : Attribute
  {
    [UsedImplicitly] public UsedImplicitlyAttribute()
      : this(ImplicitUseKindFlags.Default, ImplicitUseTargetFlags.Default) { }

    [UsedImplicitly]
    public UsedImplicitlyAttribute(ImplicitUseKindFlags useKindFlags, ImplicitUseTargetFlags targetFlags)
    {
      UseKindFlags = useKindFlags;
      TargetFlags = targetFlags;
    }

    [UsedImplicitly] public UsedImplicitlyAttribute(ImplicitUseKindFlags useKindFlags)
      : this(useKindFlags, ImplicitUseTargetFlags.Default) { }

    [UsedImplicitly] public UsedImplicitlyAttribute(ImplicitUseTargetFlags targetFlags)
      : this(ImplicitUseKindFlags.Default, targetFlags) { }

    [UsedImplicitly] public ImplicitUseKindFlags UseKindFlags { get; private set; }

    /// <summary>
    /// Gets value indicating what is meant to be used
    /// </summary>
    [UsedImplicitly] public ImplicitUseTargetFlags TargetFlags { get; private set; }
  }

  /// <summary>
  /// Should be used on attributes and causes ReSharper
  /// to not mark symbols marked with such attributes as unused (as well as by other usage inspections)
  /// </summary>
  [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
  public sealed  class MeansImplicitUseAttribute : Attribute
  {
    [UsedImplicitly] public MeansImplicitUseAttribute() 
      : this(ImplicitUseKindFlags.Default, ImplicitUseTargetFlags.Default) { }

    [UsedImplicitly]
    public MeansImplicitUseAttribute(ImplicitUseKindFlags useKindFlags, ImplicitUseTargetFlags targetFlags)
    {
      UseKindFlags = useKindFlags;
      TargetFlags = targetFlags;
    }

    [UsedImplicitly] public MeansImplicitUseAttribute(ImplicitUseKindFlags useKindFlags)
      : this(useKindFlags, ImplicitUseTargetFlags.Default)
    {
    }

    [UsedImplicitly] public MeansImplicitUseAttribute(ImplicitUseTargetFlags targetFlags)
      : this(ImplicitUseKindFlags.Default, targetFlags) { }

    [UsedImplicitly] public ImplicitUseKindFlags UseKindFlags { get; private set; }

    /// <summary>
    /// Gets value indicating what is meant to be used
    /// </summary>
    [UsedImplicitly] public ImplicitUseTargetFlags TargetFlags { get; private set; }
  }
  
  [Flags]
  public enum ImplicitUseKindFlags
  {
    Default = Access | Assign | InstantiatedWithFixedConstructorSignature,

    /// <summary>
    /// Only entity marked with attribute considered used
    /// </summary>
    Access = 1,

    /// <summary>
    /// Indicates implicit assignment to a member
    /// </summary>
    Assign = 2,

    /// <summary>
    /// Indicates implicit instantiation of a type with fixed constructor signature.
    /// That means any unused constructor parameters won't be reported as such.
    /// </summary>
    InstantiatedWithFixedConstructorSignature = 4,

    /// <summary>
    /// Indicates implicit instantiation of a type
    /// </summary>
    InstantiatedNoFixedConstructorSignature = 8,
  }

  /// <summary>
  /// Specify what is considered used implicitly when marked with <see cref="MeansImplicitUseAttribute"/> or <see cref="UsedImplicitlyAttribute"/>
  /// </summary>
  [Flags]
  public enum ImplicitUseTargetFlags
  {
    Default = Itself,

    Itself = 1,

    /// <summary>
    /// Members of entity marked with attribute are considered used
    /// </summary>
    Members = 2,

    /// <summary>
    /// Entity marked with attribute and all its members considered used
    /// </summary>
    WithMembers = Itself | Members
  }

  /// <summary>
  /// This attribute is intended to mark publicly available API which should not be removed and so is treated as used.
  /// </summary>
  [MeansImplicitUse]
  public sealed class PublicAPIAttribute : Attribute
  {
    public PublicAPIAttribute() { }
    public PublicAPIAttribute(string comment) { }
  }

  /// <summary>
  /// Tells code analysis engine if the parameter is completely handled when the invoked method is on stack. 
  /// If the parameter is a delegate, indicates that delegate is executed while the method is executed.
  /// If the parameter is an enumerable, indicates that it is enumerated while the method is executed.
  /// </summary>
  [AttributeUsage(AttributeTargets.Parameter, Inherited = true)]
  public sealed class InstantHandleAttribute : Attribute { }


  /// <summary>
  /// Indicates that a method does not make any observable state changes.
  /// The same as <see cref="System.Diagnostics.Contracts.PureAttribute"/>
  /// </summary>
  /// <example>
  /// <code>
  /// [Pure]
  /// private int Multiply(int x, int y)
  /// {
  ///   return x*y;
  /// }
  ///
  /// public void Foo()
  /// {
  ///   const int a=2, b=2;
  ///   Multiply(a, b); // Waring: Return value of pure method is not used
  /// }
  /// </code>
  /// </example>
  [AttributeUsage(AttributeTargets.Method, Inherited = true)]
  public sealed class PureAttribute : Attribute { }

  /// <summary>
  /// Indicates that a parameter is a path to a file or a folder within a web project.
  /// Path can be relative or absolute, starting from web root (~).
  /// </summary>
  [AttributeUsage(AttributeTargets.Parameter)]
  public class PathReferenceAttribute : Attribute
  {
    public PathReferenceAttribute() { }

    [UsedImplicitly]
    public PathReferenceAttribute([PathReference] string basePath)
    {
      BasePath = basePath;
    }

    [UsedImplicitly] public string BasePath { get; private set; }
  }

  // ASP.NET MVC attributes

  /// <summary>
  /// ASP.NET MVC attribute. If applied to a parameter, indicates that the parameter is an MVC action.
  /// If applied to a method, the MVC action name is calculated implicitly from the context.
  /// Use this attribute for custom wrappers similar to 
  /// <see cref="System.Web.Mvc.Html.ChildActionExtensions.RenderAction(HtmlHelper, String)"/>
  /// </summary>
  [AttributeUsage(AttributeTargets.Parameter | AttributeTargets.Method)]
  public sealed class AspMvcActionAttribute : Attribute
  {
    [UsedImplicitly] public string AnonymousProperty { get; private set; }

    public AspMvcActionAttribute() { }

    public AspMvcActionAttribute(string anonymousProperty)
    {
      AnonymousProperty = anonymousProperty;
    }
  }

  /// <summary>
  /// ASP.NET MVC attribute. Indicates that a parameter is an MVC araa.
  /// Use this attribute for custom wrappers similar to 
  /// <see cref="System.Web.Mvc.Html.ChildActionExtensions.RenderAction(HtmlHelper, String)"/>
  /// </summary>
  [AttributeUsage(AttributeTargets.Parameter)]
  public sealed class AspMvcAreaAttribute : PathReferenceAttribute
  {
    [UsedImplicitly] public string AnonymousProperty { get; private set; }

    [UsedImplicitly] public AspMvcAreaAttribute() { }

    public AspMvcAreaAttribute(string anonymousProperty)
    {
      AnonymousProperty = anonymousProperty;
    }
  }

  /// <summary>
  /// ASP.NET MVC attribute. If applied to a parameter, indicates that the parameter is an MVC controller.
  /// If applied to a method, the MVC controller name is calculated implicitly from the context.
  /// Use this attribute for custom wrappers similar to 
  /// <see cref="System.Web.Mvc.Html.ChildActionExtensions.RenderAction(HtmlHelper, String, String)"/> 
  /// </summary>
  [AttributeUsage(AttributeTargets.Parameter | AttributeTargets.Method)]
  public sealed class AspMvcControllerAttribute : Attribute
  {
    [UsedImplicitly] public string AnonymousProperty { get; private set; }

    public AspMvcControllerAttribute() { }

    public AspMvcControllerAttribute(string anonymousProperty)
    {
      AnonymousProperty = anonymousProperty;
    }
  }

  /// <summary>
  /// ASP.NET MVC attribute. Indicates that a parameter is an MVC Master.
  /// Use this attribute for custom wrappers similar to 
  /// <see cref="System.Web.Mvc.Controller.View(String, String)"/>
  /// </summary>
  [AttributeUsage(AttributeTargets.Parameter)]
  public sealed class AspMvcMasterAttribute : Attribute { }

  /// <summary>
  /// ASP.NET MVC attribute. Indicates that a parameter is an MVC model type.
  /// Use this attribute for custom wrappers similar to 
  /// <see cref="System.Web.Mvc.Controller.View(String, Object)"/>
  /// </summary>
  [AttributeUsage(AttributeTargets.Parameter)]
  public sealed class AspMvcModelTypeAttribute : Attribute { }

  /// <summary>
  /// ASP.NET MVC attribute. If applied to a parameter, indicates that the parameter is an MVC partial view.
  /// If applied to a method, the MVC partial view name is calculated implicitly from the context.
  /// Use this attribute for custom wrappers similar to 
  /// <see cref="System.Web.Mvc.Html.RenderPartialExtensions.RenderPartial(HtmlHelper, String)"/>
  /// </summary>
  [AttributeUsage(AttributeTargets.Parameter | AttributeTargets.Method)]
  public sealed class AspMvcPartialViewAttribute : PathReferenceAttribute { }

  /// <summary>
  /// ASP.NET MVC attribute. Allows disabling all inspections for MVC views within a class or a method.
  /// </summary>
  [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
  public sealed class AspMvcSupressViewErrorAttribute : Attribute { }

  /// <summary>
  /// ASP.NET MVC attribute. Indicates that a parameter is an MVC display template.
  /// Use this attribute for custom wrappers similar to 
  /// <see cref="System.Web.Mvc.Html.DisplayExtensions.DisplayForModel(HtmlHelper, String)"/>
  /// </summary>
  [AttributeUsage(AttributeTargets.Parameter)]
  public sealed class AspMvcDisplayTemplateAttribute : Attribute { }

  /// <summary>
  /// ASP.NET MVC attribute. Indicates that a parameter is an MVC editor template.
  /// Use this attribute for custom wrappers similar to 
  /// <see cref="System.Web.Mvc.Html.EditorExtensions.EditorForModel(HtmlHelper, String)"/>
  /// </summary>
  [AttributeUsage(AttributeTargets.Parameter)]
  public sealed class AspMvcEditorTemplateAttribute : Attribute { }

  /// <summary>
  /// ASP.NET MVC attribute. If applied to a parameter, indicates that the parameter is an MVC view.
  /// If applied to a method, the MVC view name is calculated implicitly from the context.
  /// Use this attribute for custom wrappers similar to 
  /// <see cref="System.Web.Mvc.Controller.View(Object)"/>
  /// </summary>
  [AttributeUsage(AttributeTargets.Parameter | AttributeTargets.Method)]
  public sealed class AspMvcViewAttribute : PathReferenceAttribute { }

  /// <summary>
  /// ASP.NET MVC attribute. When applied to a parameter of an attribute,
  /// indicates that this parameter is an MVC action name.
  /// </summary>
  /// <example>
  /// <code>
  /// [ActionName("Foo")]
  /// public ActionResult Login(string returnUrl)
  /// {
  ///   ViewBag.ReturnUrl = Url.Action("Foo"); // OK
  ///   return RedirectToAction("Bar"); // Error: Cannot resolve action
  /// }
  /// </code>
  /// </example>
  [AttributeUsage(AttributeTargets.Parameter | AttributeTargets.Property)]
  public sealed class AspMvcActionSelectorAttribute : Attribute { }

  // Razor attributes

  /// <summary>
  /// Razor attribute. Indicates that a parameter or a method is a Razor section.
  /// Use this attribute for custom wrappers similar to 
  /// <see cref="System.Web.WebPages.WebPageBase.RenderSection(String)"/>
  /// </summary>
  [AttributeUsage(AttributeTargets.Parameter | AttributeTargets.Method, Inherited = true)]
  public sealed class RazorSectionAttribute : Attribute { }

}