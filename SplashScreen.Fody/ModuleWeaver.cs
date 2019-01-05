﻿namespace SplashScreen.Fody
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using FodyTools;

    using JetBrains.Annotations;

    using Mono.Cecil;
    using Mono.Cecil.Cil;
    using Mono.Cecil.Rocks;

    public class ModuleWeaver : AbstractModuleWeaver
    {
        private const string SplashScreenAttributeName = "SplashScreen.SplashScreenAttribute";
        private const string MinimumVisibilityDurationPropertyName = "MinimumVisibilityDuration";
        private const string FadeoutDurationPropertyName = "FadeoutDuration";
        private const string SplashScreenAdapterTypeName = "SplashScreenAdapter";

        private const string SplashResourceName = "splash_a7675be0ade04430a1bd47ee14b34343.png";

        public override void Execute()
        {
            // System.Diagnostics.Debugger.Launch();

            var entryPoint = ModuleDefinition.EntryPoint;

            if (entryPoint == null)
            {
                LogError("No entry point found in target module.");
                return;
            }

            TypeDefinition splashScreenControl;

            try
            {
                splashScreenControl = ModuleDefinition.Types.Single(HasSplashScreenAttribute);
            }
            catch (Exception ex)
            {
                LogError("No single class with the [SplashScreen] attribute found: " + ex.Message);
                return;
            }

            byte[] bitmapData;

            try
            {
                bitmapData = BitmapGenerator.Generate(AddinDirectoryPath, AssemblyFilePath, splashScreenControl.FullName, ReferenceCopyLocalPaths);
            }
            catch (Exception ex)
            {
                LogError(ex.Message);
                return;
            }

            var splashScreenControlBamlResourceName = splashScreenControl.Name.ToLowerInvariant() + ".baml";

            ResourceHelper.UpdateResources(ModuleDefinition, SplashResourceName, bitmapData, splashScreenControlBamlResourceName);

            ModuleDefinition.Types.Remove(splashScreenControl);

            var importer = new CodeImporter(ModuleDefinition);

            var attribute = GetSplashScreenAttribute(splashScreenControl);

            var minimumVisibilityDuration = attribute.GetPropertyValue(MinimumVisibilityDurationPropertyName, 4.0);
            var fadeoutDuration = attribute.GetPropertyValue(FadeoutDurationPropertyName, 1.0);

            var referencedModule = attribute.AttributeType.Resolve().Module;

            var adapterType = referencedModule.Types.Single(type => type.Name == SplashScreenAdapterTypeName);

            adapterType = importer.Import(adapterType);

            importer.ILMerge();

            var adapterTypeConstructor = adapterType.GetConstructors().Single(ctor => ctor.Parameters.Count == 3);

            entryPoint.Body.Instructions.InsertRange(0,
                Instruction.Create(OpCodes.Ldstr, SplashResourceName),
                Instruction.Create(OpCodes.Ldc_R8, minimumVisibilityDuration),
                Instruction.Create(OpCodes.Ldc_R8, fadeoutDuration),
                Instruction.Create(OpCodes.Newobj, ModuleDefinition.ImportReference(adapterTypeConstructor)),
                Instruction.Create(OpCodes.Pop)
                );
        }

        [NotNull]
        public override IEnumerable<string> GetAssembliesForScanning() => Enumerable.Empty<string>();

        public override bool ShouldCleanReference => true;

        private bool HasSplashScreenAttribute(TypeDefinition type)
        {
            return null != GetSplashScreenAttribute(type);
        }

        private static CustomAttribute GetSplashScreenAttribute(ICustomAttributeProvider type)
        {
            return type.GetAttribute(SplashScreenAttributeName);
        }
    }
}
