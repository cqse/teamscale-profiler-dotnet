using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Reflection;

namespace ProfilerTest
{
    /// <summary>
    /// A test-case generator for testing the .NET method profiler.
    /// It generates a .NET console application that performs all kinds of method invocations possible in C#.
    /// </summary>
    class TestGenerator
    {
        /// <summary>
        /// Path to the Test-project. Used for debugging the generated tests.
        /// </summary>
        private const String Outpath = @"..\..\..\Test\CoverageProfilee.cs";

        /// <summary>
        /// All visibility modifiers used in C#.
        /// </summary>
        private static String[] VisibilityModifier = new String[] { "public", "private", "protected", "internal", "protected internal" };
           
        /// <summary>
        /// All class instance modifiers used for methods in C#.
        /// </summary>
        private static String[] ClassInstanceModifier = new String[] { "", "static" };
        
        /// <summary>
        /// Primitive types in C# (a selection to avoid an explosion of irrelevant tests - too many will cause the test-case to crash).
        /// </summary>
        private static String[] PrimitiveParameterTypes = new String[] { "int", "uint", "bool", "float", "double", "byte", "int*", "double*", "String" };
        
        /// <summary>
        /// Complex parameter types used for testing.
        /// </summary>
        private static String[] ComplexParameterTypes = new String[] { "object", "OuterClass<int>.InnerClass<String, int>", "OuterClass<int>.InnerStruct<int, Dictionary<String, int>>", "Dictionary<String, Dictionary<String, int>>" };
        
        /// <summary>
        /// Extension of types to be nullable. 
        /// </summary>
        private static String[] NullableTypes = new String[] { "", "?" };
        
        /// <summary>
        /// Parameter passing types in C#.
        /// </summary>
        private static String[] ParameterCallTypes = new String[] { "", "ref", "out" };
        
        /// <summary>
        /// Arrays of types. 
        /// </summary>
        private static String[] Arrays = new String[] { "", "[]", "[][]" };
        
        /// <summary>
        /// Extensions to make methods generic.
        /// </summary>
        private static String[] GenericMethod = new String[] { "", "<T>" };

        static void Main(string[] args)
        {
            // The names of the classes to generate
            String methodInvClass = "MethodInvClass";
            String methodReflectionInvClass = "ReflectionInvClass";
            String delegateInvClass = "DelegateInvClass";
            String delegateAsyncInvClass = "DelegateAsyncInvClass";

            // instantiate all different types of methods
            LinkedList<Method> methods = GenerateMethods();

            TextWriter tw = new StreamWriter(Outpath);
            tw.WriteLine("using System;");
            tw.WriteLine("using System.Reflection;");
            tw.WriteLine("using System.Collections.Generic;");
            tw.WriteLine("");
            tw.WriteLine("namespace Test{");
            tw.WriteLine("");

            GenerateGenericClasses(tw);

            tw.WriteLine("class Program{");

            tw.WriteLine("class BaseClass{");
            // generate methods to override
            tw.WriteLine("}");

            tw.WriteLine("");
            LinkedList<Method> directlyCalledMethods = EmitCodeForClassWithMethodDeclarations(methodInvClass, methods, tw, false);
            LinkedList<Method> reflectionCalledMethods = EmitCodeForClassWithMethodDeclarations(methodReflectionInvClass, methods, tw, true);
            LinkedList<Method> delegateCalledMethods = EmitCodeForClassWithMethodAndDelegateDeclarations(delegateInvClass, methods, tw, false);
            LinkedList<Method> delegateAsyncCalledMethods = EmitCodeForClassWithMethodAndDelegateDeclarations(delegateAsyncInvClass, methods, tw, true);

            tw.WriteLine("static unsafe void Main(string[] args){");
            tw.WriteLine("Program p = new Program();");

            EmitDeclareLocalVariables(new String[] { methodReflectionInvClass, methodInvClass, delegateInvClass, delegateAsyncInvClass }, tw);

            // generate different kinds of method calls
            GenerateReflectiveCallsToMethods(methodReflectionInvClass, tw, reflectionCalledMethods);
            GenerateDirectCallsToMethods(methodInvClass, tw, directlyCalledMethods);
            GenerateDelegateInvocations(delegateInvClass, delegateAsyncInvClass, tw);

            tw.WriteLine("calledRemoteCallMethod();");

            // Generate interop
            GenerateInteropCall(tw);

            tw.WriteLine("Console.WriteLine(\"SUCCESS\");");
            // End main
            tw.WriteLine("}");

            GenerateManuallyLoadedAssemblyMethodCall(tw);

            tw.WriteLine("public static void CallbackMethod(IAsyncResult result){}");
            tw.WriteLine("}");
            tw.WriteLine("");

            // end namespace
            tw.WriteLine("}");
            tw.Close();
        }

        private static void GenerateDirectCallsToMethods(String methodInvClass, TextWriter tw, LinkedList<Method> directlyCalledMethods)
        {
            tw.WriteLine(methodInvClass + "Var.CalledMethodCallsPrivateMethods();");
            foreach (Method m in directlyCalledMethods)
            {
                if (!m.ClassInstanceModifier.Equals("static")) tw.WriteLine(methodInvClass + "Var." + m.GetInvocation() + ";");
                else tw.WriteLine(methodInvClass + "<int>." + m.GetInvocation() + ";");
            }
        }

        private static void GenerateReflectiveCallsToMethods(String methodReflectionInvClass, TextWriter tw, LinkedList<Method> reflectionCalledMethods)
        {
            tw.WriteLine(methodReflectionInvClass + "Var.CalledMethodCallsPrivateMethods();");
            foreach (Method m in reflectionCalledMethods)
            {
                tw.WriteLine(m.GetReflectionInvocation(methodReflectionInvClass) + ";");
            }
        }

        private static void GenerateDelegateInvocations(String delegateInvClass, String delegateAsyncInvClass, TextWriter tw)
        {
            // generate delegate calls to methods sync (Delegates muessen immer in der Klasse aufgerufen werden)
            tw.WriteLine(delegateInvClass + "Var.CalledMethodCallsPrivateMethods();");

            // generate delegate calls to methods async
            tw.WriteLine(delegateAsyncInvClass + "Var.CalledMethodCallsPrivateMethods();");
        }

        private static void GenerateInteropCall(TextWriter tw)
        {
            /*
            tw.WriteLine("try { Microsoft.Win32.Registry.LocalMachine.GetValueKind(\"abc\"); } catch (Exception e) { Console.WriteLine(e.StackTrace); }");
            tw.WriteLine("Microsoft.Office.Interop.PowerPoint.ApplicationClass app = new Microsoft.Office.Interop.PowerPoint.ApplicationClass();");
            tw.WriteLine("app.Quit();");
            */
        }

        private static void GenerateManuallyLoadedAssemblyMethodCall(TextWriter tw)
        {
            tw.WriteLine("public static void calledRemoteCallMethod(){");
            tw.WriteLine("System.AppDomain NewAppDomain = System.AppDomain.CreateDomain(\"NewApplicationDomain\");");
            tw.WriteLine("Assembly a = NewAppDomain.Load(@\"RemotelyCalledAssembly\");");
            tw.WriteLine("Type myType = a.GetType(\"RemotelyCalledAssembly.RemoteClass\");");
            tw.WriteLine("MethodInfo mymethod = myType.GetMethod(\"calledRemoteMethod\");");
            tw.WriteLine("Object obj = Activator.CreateInstance(myType);");
            tw.WriteLine("mymethod.Invoke(obj, null);");
            tw.WriteLine("System.AppDomain.Unload(NewAppDomain);");
            tw.WriteLine("}");
        }

        private static void GenerateGenericClasses(TextWriter tw)
        {
            tw.WriteLine("public class OuterClass<C>{");
            tw.WriteLine("public class InnerClass<T,B>{ public InnerClass(){new OuterClass<int>();}}");
            tw.WriteLine("public class InnerStruct<T,B>{}");
            tw.WriteLine("}");
        }

        private static LinkedList<Method> EmitCodeForClassWithMethodDeclarations(String className, LinkedList<Method> methods, TextWriter tw, bool reflection)
        {
            tw.WriteLine("class " + className + "<T> : BaseClass{");
            // generate methods 
            foreach (Method m in methods)
            {
                if (reflection && (m.PrimitiveParameterType.Contains("*") || m.ComplexParameterType.Contains("*") || !m.VisibilityModifier.Equals("public")))
                {
                    continue;
                }
                tw.WriteLine(m.GetDeclaration());
            }

            LinkedList<Method> ExternMethods = EmitCodeForIndirectCalls(className, methods, tw, reflection);

            tw.WriteLine("}");
            return ExternMethods;
        }

        private static LinkedList<Method> EmitCodeForIndirectCalls(String className, LinkedList<Method> methods, TextWriter tw, bool reflection)
        {
            LinkedList<Method> externallyCalledMethods = new LinkedList<Method>();

            tw.WriteLine("");
            tw.WriteLine("public unsafe void CalledMethodCallsPrivateMethods(){");
            EmitDeclareLocalVariables(new String[]{className}, tw);
            foreach (Method m in methods)
            {
                if (reflection && (m.PrimitiveParameterType.Contains("*") || m.ComplexParameterType.Contains("*") || !m.VisibilityModifier.Equals("public"))) continue;

                if (!(m.VisibilityModifier.Equals("private") || m.VisibilityModifier.Equals("protected")))
                {
                    externallyCalledMethods.AddFirst(m);
                    continue;
                }
                if (reflection)
                {
                    tw.WriteLine(m.GetReflectionInvocation(className));
                }
                else
                {
                    if (!m.ClassInstanceModifier.Equals("static"))
                    {
                        tw.WriteLine(className + "Var." + m.GetInvocation() + ";");
                    }
                    else
                    {
                        tw.WriteLine(className + "<int>." + m.GetInvocation() + ";");
                    }
                }
            }
            tw.WriteLine("}");
            return externallyCalledMethods;
        }

        private static LinkedList<Method> EmitCodeForClassWithMethodAndDelegateDeclarations(String className, LinkedList<Method> methods, TextWriter tw, bool async)
        {
            Dictionary<String, LinkedList<Method>> DelegateMapping = new Dictionary<String, LinkedList<Method>>();
            tw.WriteLine("class " + className + "<T> : BaseClass{");
            // generate methods 
            foreach (Method m in methods)
            {
                if (async && (m.PrimitiveParameterType.Contains("*") || m.ComplexParameterType.Contains("*")))
                {
                    continue;
                }
                String hash = m.GetDelegateHash(async);
                if (!DelegateMapping.ContainsKey(hash))
                {
                    tw.WriteLine(m.GetDelegate());
                    tw.WriteLine(m.GetEvent());
                    DelegateMapping.Add(hash, new LinkedList<Method>());
                    DelegateMapping[hash].AddFirst(m);
                }
                else
                {
                    DelegateMapping[hash].AddFirst(m);
                }
                tw.WriteLine(m.GetDeclaration());
            }

            LinkedList<Method> ExternMethods = EmitCodeForIndirectDelegateCalls(className, tw, async, DelegateMapping);

            tw.WriteLine("}");
            return ExternMethods;
        }

        private static LinkedList<Method> EmitCodeForIndirectDelegateCalls(String className, TextWriter tw, bool async, Dictionary<String, LinkedList<Method>> DelegateMapping)
        {
            LinkedList<Method> externallyCalledEvents = new LinkedList<Method>();
            LinkedList<Method> events = new LinkedList<Method>();

            tw.WriteLine("");
            tw.WriteLine("public unsafe void CalledMethodCallsPrivateMethods(){");
            EmitDeclareLocalVariables(new String[]{className}, tw);

            // Register EventHandlers
            foreach (String hash in DelegateMapping.Keys)
            {
                Method delegateProvider = DelegateMapping[hash].Last.Value;
                if(!async) events.AddFirst(delegateProvider);

                // Im async Fall genau ein Durchlauf
                foreach(Method m in DelegateMapping[hash]){           
                    tw.WriteLine(delegateProvider.GetEventHandlerRegistration(m, async));
                }

                // Einkommentieren fuer async
                if (async)
                {
                    if (!delegateProvider.ClassInstanceModifier.Equals("static"))
                    {
                        tw.WriteLine(delegateProvider.GetEventFireCode("", async));
                    }
                    else
                    {
                        tw.WriteLine(delegateProvider.GetEventFireCode(className + "<int>.", async));
                    }
                }
            }

            // Fire Events -  wird im async Fall nie durchlaufen
            foreach (Method m in events)
            {
                if (!m.ClassInstanceModifier.Equals("static")) tw.WriteLine(m.GetEventFireCode("", async));
                else tw.WriteLine(m.GetEventFireCode(className + "<int>.", async));
            }
            tw.WriteLine("}");
            return externallyCalledEvents;
        }

        private static void EmitDeclareLocalVariables(String[] classNames, TextWriter tw)
        {
            tw.WriteLine("MethodInfo methodInfo;");
            tw.WriteLine("ParameterModifier paramMod = new ParameterModifier(2);");
            foreach (String className in classNames)
            {
                tw.WriteLine(className + "<int> " + className + "Var = new " + className + "<int>();");
                tw.WriteLine("Type " + className + "Type = typeof( " + className + "<int>);");
            }

            foreach (String array in Arrays)
            {
                foreach (String primitiveType in PrimitiveParameterTypes)
                {
                    tw.WriteLine(primitiveType + array + " " + RemoveChars(primitiveType + array) + "Var = " + GetPrimitiveInstance(primitiveType, array) + ";");
                    if (primitiveType.Equals("String") || primitiveType.Contains("*")) continue;
                    tw.WriteLine(primitiveType + "? " + array + RemoveChars(primitiveType + "?" + array) + "Var = " + GetPrimitiveInstance(primitiveType + "?", array) + ";");
                }
                foreach (String complexType in ComplexParameterTypes)
                {
                    // hier unterscheiden, ob array
                    String instantiation = "new " + complexType + "();";
                    if (array.Contains("[]"))
                    {
                        instantiation = "new " + complexType + array + "{};";
                    }
                    tw.WriteLine(complexType + array + " " + RemoveChars(complexType + array) + "Var = " + instantiation);
                }
            }
        }

        private static LinkedList<Method> GenerateMethods()
        {
            LinkedList<Method> methods = new LinkedList<Method>();

            int primitveTypeIndex = 0;

            foreach (String visibility in VisibilityModifier)
            {
                foreach (String generic in GenericMethod)
                {
                    foreach (String staticModifier in ClassInstanceModifier)
                    {
     //                   foreach (String primitiveType in PrimitiveParameterTypes)
                        {
                            foreach (String refType in ParameterCallTypes)
                            {
                                foreach (String nullable in NullableTypes)
                                {
                                    foreach (String complexType in ComplexParameterTypes)
                                    {
                                        // nullable Strings or pointers must not appear as parameters
                                        if (nullable.Equals("?") && (PrimitiveParameterTypes[primitveTypeIndex].Equals("String") || PrimitiveParameterTypes[primitveTypeIndex].Contains("*"))) continue;

                                        foreach (String arrayType in Arrays)
                                        {
                                            Method m = new Method(visibility, "", staticModifier, PrimitiveParameterTypes[primitveTypeIndex], complexType, nullable, refType, generic, arrayType, "calledMethod" + (methods.Count + 1));
                                            primitveTypeIndex++;
                                            primitveTypeIndex = primitveTypeIndex % PrimitiveParameterTypes.Length;
                                            methods.AddFirst(m);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            return methods;
        }

        public static string RemoveChars(string complexType)
        {
            String str = complexType;
            str = str.Replace(" ","");
            str = str.Replace("<","");
            str = str.Replace(">", "");
            str = str.Replace(",", "");
            str = str.Replace("?", "Nullable");
            str = str.Replace("*", "Ptr");
            str = str.Replace("[]", "Array");
            str = str.Replace(".", "_");

            return str;
        }

        public static string GetPrimitiveInstance(string primitiveParameterType, string array)
        {
            String str = String.Empty;

            if (array.Contains("[]"))
            {
                str += "new " + primitiveParameterType + array+"{"  + "}";
                return str;
            }

            if (primitiveParameterType.Contains("*"))
            {
                str += "(" + primitiveParameterType + ")5";
                return str;
            }
            if (primitiveParameterType.Contains("int") || primitiveParameterType.StartsWith("byte") || primitiveParameterType.StartsWith("double") || primitiveParameterType.StartsWith("float"))
            {
                str += "5";
            }
            else if (primitiveParameterType.StartsWith("String"))
            {
                str += "\"Hallo\"";
            }
            else if (primitiveParameterType.StartsWith("bool"))
            {
                str += "true";
            }
            return str;
        }
    }


    class Method
    {

        internal String VisibilityModifier;
        internal String InheritanceModifier;
        internal String ClassInstanceModifier;
        internal String PrimitiveParameterType;
        internal String ComplexParameterType;
        internal String NullableType;
        internal String ParameterCallType;
        internal String GenericMethod;
        internal String Name;
        internal String Arrays;

        public Method(String visibilityModifier, String inheritanceModifier, String classInstanceModifier, String primitiveParameterType, String complexParameterType, String nullableType, String parameterCallType, String genericMethod, String arrays, String name)
        {
            VisibilityModifier = visibilityModifier;
            InheritanceModifier = inheritanceModifier;
            ClassInstanceModifier = classInstanceModifier;
            PrimitiveParameterType = primitiveParameterType + nullableType;
            ComplexParameterType = complexParameterType;
            NullableType = nullableType;
            ParameterCallType = parameterCallType;
            GenericMethod = genericMethod;
            Arrays = arrays;
            Name = name;
        }

        public String GetDelegateHash(bool async)
        {
            String hash = PrimitiveParameterType + ComplexParameterType + ParameterCallType + GenericMethod+Arrays;
            if (async) return hash += Name;
            return hash;
        }

        public String GetDelegate()
        {
            String safety = String.Empty;
            if (PrimitiveParameterType.Contains("*")) safety = "unsafe";

            String heading = VisibilityModifier + " " + safety + " delegate void " + Name + "EventHandler" + GenericMethod.Replace('T', 'B') + "(" + ParameterCallType + " " + ComplexParameterType + Arrays + " x, " + ParameterCallType + " " + PrimitiveParameterType + Arrays + " y);";
            
            return heading;
        }

        public String GetEvent()
        {
            String safety = String.Empty;
            if (PrimitiveParameterType.Contains("*")) safety = "unsafe";

            String generic = String.Empty;
            if (GenericMethod.Equals("<T>")) generic += "<int>";

            String heading = VisibilityModifier + " " + ClassInstanceModifier + " " + safety + " event " + Name + "EventHandler" + generic + " " + Name + "Trigger" + ";";

            return heading;
        }

        public String GetEventHandlerRegistration(Method m, bool async)
        {
            String genericHandler = String.Empty;
            if (GenericMethod.Equals("<T>")) genericHandler += "<int>";
            
            String genericMethod = String.Empty;
            if (m.GenericMethod.Equals("<T>")) genericMethod += "<int>";

            String instanceIdentifier = Name + "EventHandlerInstance_" + m.Name;
            if (async) instanceIdentifier += "_Async";
            String intantiateHandler = Name + "EventHandler" + genericHandler + " " +instanceIdentifier+ " = new " + Name + "EventHandler" + genericHandler + "(" + m.Name + genericMethod + "); ";

            String addHandlerCode = Name + "Trigger += " + instanceIdentifier + "; ";
            String removeHandlerCode = Name + "Trigger -= " + instanceIdentifier + "; ";

            return intantiateHandler + addHandlerCode + removeHandlerCode + addHandlerCode;
        }

        public String GetEventFireCode(String variable, bool async)
        {
            String str = String.Empty;
            String resultIdentifier = String.Empty;
            if (async)
            {
                resultIdentifier = "result" + Name + "Trigger";
                str = "IAsyncResult " + resultIdentifier +" = ";
            }

            String eventName = variable + Name + "Trigger";
            str += eventName;

            if (async)
            {
                str += ".BeginInvoke";
            }

            String parameter = ParameterCallType + " " + TestGenerator.RemoveChars(ComplexParameterType + Arrays) + "Var, " + ParameterCallType + " " + TestGenerator.RemoveChars(PrimitiveParameterType + Arrays) + "Var";
            str += "(" + parameter;

            if (async)
            {
                str += ", new AsyncCallback(Program.CallbackMethod), new object()";
            }
            
            str += ");";

            if (async)
            {
                str += eventName + ".EndInvoke(";
                if (!ParameterCallType.Equals(String.Empty)) str += parameter+",";
                str += resultIdentifier + ");";
            }

            return str; 
        }


        public String GetDeclaration(){
            String safety = String.Empty;

            if (PrimitiveParameterType.Contains("*")) safety = "unsafe";

            String heading = VisibilityModifier + " " + ClassInstanceModifier + " " + safety + " void " + Name + GenericMethod.Replace('T', 'B') + "(" + ParameterCallType + " " + ComplexParameterType + Arrays + " x, " + ParameterCallType + " " + PrimitiveParameterType + Arrays + " y)";
            String str = heading+"{";
            if(!Arrays.Contains("[]"))str += "x = new " + ComplexParameterType + "();";
            else str += "x = new " + ComplexParameterType +Arrays +"{};";
            str += "y = " + TestGenerator.GetPrimitiveInstance(PrimitiveParameterType, Arrays) + ";";
            str += "Console.WriteLine(\"Called " + heading + "\");";
            str += "}";

            return str;
        }


        public String GetInvocation()
        {
            String str = Name;

            if (GenericMethod.Equals("<T>")) str += "<int>";
            str += "(" + GetInvocationArgumentString() + ")";

            return str; 
        }

        private String GetInvocationArgumentString()
        {
            return ParameterCallType + " " + TestGenerator.RemoveChars(ComplexParameterType + Arrays) + "Var, " + ParameterCallType + " " + TestGenerator.RemoveChars(PrimitiveParameterType + Arrays) + "Var";
        }

        internal string GetReflectionInvocation(string methodInvClass)
        {
            String str = String.Empty;
            str += "object[] args_" + Name + " = {" + TestGenerator.RemoveChars(ComplexParameterType + Arrays) + "Var, " + TestGenerator.RemoveChars(PrimitiveParameterType + Arrays) + "Var};";            
            str += "methodInfo = "+methodInvClass+"Type.GetMethod(\""+Name+"\"); ";

            if (GenericMethod.Equals("<T>"))
            {
                str += "Type[] genericArgumentsOf" + Name + " = new Type[] { typeof(int) };";
                str += "methodInfo = methodInfo.MakeGenericMethod(genericArgumentsOf" + Name + ");";
            }

            String firstParam = String.Empty;    
            if(ClassInstanceModifier.Equals("static")){
                    firstParam = "null";
            }
            else { firstParam = methodInvClass+"Var"; }

            str += "methodInfo.Invoke(" + firstParam + ", args_" + Name + ");";

            return str;
        }
    }
}