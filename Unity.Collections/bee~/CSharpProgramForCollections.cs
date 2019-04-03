using Bee.Toolchain.Emscripten;
using Newtonsoft.Json.Linq;
using NiceIO;
using Unity.BuildSystem.NativeProgramSupport;

[CSharpProgramForModule("Unity.Collections")]
class CSharpProgramForCollections : CSharpProgramForAsmDef
{
    public CSharpProgramForCollections(bool forceExe, JObject json, NPath asmDef) : base(forceExe, json, asmDef)
    {
    }

    protected override void SetupReferences()
    {
        base.SetupReferences();
        References.Add(AsmDef.Parent.Parent.Combine("System.Runtime.CompilerServices.Unsafe.dll"));
	    References.Add(BuildProgram.BeeRoot.Combine("CSharpSupport/Framework-v4.5-Facades/System.Runtime.Modified.dll"));
    }
}
