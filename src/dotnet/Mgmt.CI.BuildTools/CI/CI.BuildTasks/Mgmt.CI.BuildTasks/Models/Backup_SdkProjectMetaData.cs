﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

namespace MS.Az.NetSdk.Build.Models
{
    using Microsoft.Build.Construction;
    using Microsoft.Build.Evaluation;
    using Microsoft.Build.Framework;
    using MS.Az.Mgmt.CI.BuildTasks.Common;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using MS.Az.Mgmt.CI.BuildTasks.Common.ExtensionMethods;
    using MS.Az.Mgmt.CI.BuildTasks.Common.Logger;

    public class Backup_SdkProjectMetaData
    {
        public TargetFrameworkMoniker FxMoniker { get; set; }

        public string FxMonikerString { get; set; }
        public string FullProjectPath { get; set; }

        public string TargetOutputFullPath { get; set; }

        public bool IsTargetFxSupported { get; set; }

        public SdkProjectType ProjectType { get; set; }

        public ITaskItem ProjectTaskItem { get; set; }

        public Backup_SdkProjectMetaData() { }

        public Project MsBuildProject { get; set; }

        public bool IsProjectDataPlane { get; set; }

        public bool IsFxFullDesktopVersion { get; set; }

        public bool IsFxNetCore { get; set; }

        public bool IsNonSdkProject { get; set; }

        public List<string> ProjectImports { get; set; }

        public List<string> PackageReferences { get; set; }

        NetSdkBuildTaskLogger UtilLogger { get; set; }

        #region Constructors
        public SdkProjectMetaData(ITaskItem projTaskItem) : this(projTaskItem.ItemSpec)
        {

        }

        public SdkProjectMetaData(ITaskItem project, Project msbuildProject, TargetFrameworkMoniker fxMoniker, 
            string fxMonikerString, string fullProjectPath, string targetOutputPath, bool isTargetFxSupported, 
            SdkProjectType projectType = SdkProjectType.Sdk)
        {
            ProjectTaskItem = project;
            FxMoniker = fxMoniker;
            FullProjectPath = fullProjectPath;
            //IsTargetFxSupported = isTargetFxSupported;
            IsTargetFxSupported = IsFxSupported(fxMonikerString);
            ProjectType = projectType;
            TargetOutputFullPath = targetOutputPath;
            FxMonikerString = fxMonikerString;
            MsBuildProject = msbuildProject;
        }

        public SdkProjectMetaData(ITaskItem project, 
                                    Project msbuildProject, TargetFrameworkMoniker fxMoniker, string fxMonikerString, 
                                    string fullProjectPath, string targetOutputPath, bool isTargetFxSupported, 
                                    SdkProjectType projectType,
                                    bool isProjectDataPlaneProject,
                                    bool isNonSdkProject = true)
        {
            //, bool isTargetFxFullDesktop, bool isTargetNetCore

            ProjectTaskItem = project;
            FxMoniker = fxMoniker;
            FullProjectPath = fullProjectPath;
            //IsTargetFxSupported = isTargetFxSupported;
            ProjectType = projectType;
            TargetOutputFullPath = targetOutputPath;
            FxMonikerString = fxMonikerString;
            IsTargetFxSupported = IsFxSupported(fxMonikerString);
            MsBuildProject = msbuildProject;
            IsProjectDataPlane = isProjectDataPlaneProject;
            IsFxFullDesktopVersion = IsExpectedFxCategory(fxMoniker, TargetFxCategory.FullDesktop);
            IsFxNetCore = IsExpectedFxCategory(fxMoniker, TargetFxCategory.NetCore);
            IsNonSdkProject = isNonSdkProject;
        }

        public SdkProjectMetaData(string fullProjectPath, TargetFrameworkMoniker priorityFxVersion = TargetFrameworkMoniker.net452)
        {
            if(!string.IsNullOrEmpty(fullProjectPath))
            {
                MsBuildProject = GetProject(fullProjectPath);

                if(MsBuildProject != null)
                {
                    FxMoniker = GetTargetFramework(MsBuildProject, priorityFxVersion);
                    FxMonikerString = GetFxMonikerString(priorityFxVersion);
                    ProjectTaskItem = new Microsoft.Build.Utilities.TaskItem(fullProjectPath);
                    FullProjectPath = fullProjectPath;
                    TargetOutputFullPath = GetTargetFullPath(MsBuildProject, FxMonikerString);                    
                    ProjectType = GetProjectType(MsBuildProject);
                    IsTargetFxSupported = IsFxSupported(FxMonikerString);
                    IsProjectDataPlane = IsDataPlaneProject(MsBuildProject);
                    IsFxFullDesktopVersion = IsExpectedFxCategory(FxMoniker, TargetFxCategory.FullDesktop);
                    IsFxNetCore = IsExpectedFxCategory(FxMoniker, TargetFxCategory.NetCore);
                    ProjectImports = GetProjectImports(MsBuildProject);
                }
                else
                {
                    throw new NullReferenceException("MsBuild Project null");
                }
            }
        }
        #endregion

        #region Public Functions
        public List<SdkProjectMetaData> GetSupportedProjects(List<SdkProjectMetaData> projectList, SdkProjectType projectType)
        {
            return null;
        }

        public List<SdkProjectMetaData> GetUnsupportedProjectList(List<SdkProjectMetaData> projectList, SdkProjectType projectType)
        {
            return null;
        }

        public List<SdkProjectMetaData> GetFxBasedProjectList(List<SdkProjectMetaData> projectList, SdkProjectType projectType, 
                                                                                        bool findSupportedFx)
        {
            List<SdkProjectMetaData> projList = new List<SdkProjectMetaData>();

            foreach(SdkProjectMetaData proj in projectList)
            {

            }

            return projList;
        }

        private string GetProjectPropertyVaule(SdkProjectMetaData proj, string propertyName)
        {
            string fx = string.Empty;
            // We are not handling a scenario where TargetFx will not be set
            // This can happen when a project has not imported *.props
            // We are not handling that situation
            if (propertyName.Contains("TargetFramework", StringComparison.OrdinalIgnoreCase))
            {
                fx = proj.MsBuildProject.GetPropertyValue("TargetFrameworks");
                if(string.IsNullOrWhiteSpace(fx))
                {
                    fx = proj.MsBuildProject.GetPropertyValue("TargetFramework");
                }
            }

            return fx;
        }

        #endregion

        private Project GetProject(string fullProjectPath)
        {
            Project proj = null;
            //try
            //{
                if (ProjectCollection.GlobalProjectCollection.Count > 0)
                {
                    proj = ProjectCollection.GlobalProjectCollection.GetLoadedProjects(fullProjectPath).FirstOrDefault<Project>();
                }

                if(proj == null)
                {
                    proj = new Project(fullProjectPath);
                }
            //}
            //catch (Exception ex)
            //{

            //}

            return proj;
        }
        
        private List<string> GetProjectImports(Project msbuildProj)
        {
            string rpProps = CommonConstants.BuildStageConstant.PROPS_APITAG_FILE_NAME;
            string multiApiProps = CommonConstants.BuildStageConstant.PROPS_MULTIAPITAG_FILE_NAME;
            //$([MSBuild]::GetPathOfFileAbove('AzSdk.RP.props'))
            List<string> importList = new List<string>();
            ProjectRootElement rootElm = msbuildProj.Xml;
            ICollection<ProjectImportElement> importElms = rootElm.Imports;

            foreach (ProjectImportElement imp in importElms)
            {
                if (imp.Project.Contains(rpProps))
                {
                    importList.Add(rpProps);
                }

                if (imp.Project.Contains(multiApiProps))
                {
                    importList.Add(multiApiProps);
                }
            }

            return importList;
        }

        private TargetFrameworkMoniker GetTargetFramework(Project msBuildProj, TargetFrameworkMoniker priorityFxVersion)
        {
            TargetFrameworkMoniker moniker = TargetFrameworkMoniker.UnSupported;
            string targetFxList = msBuildProj.GetPropertyValue("TargetFrameworks");
            if (string.IsNullOrEmpty(targetFxList))
            {
                targetFxList = msBuildProj.GetPropertyValue("TargetFramework");
            }

            var fxNames = targetFxList?.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries)?.ToList<string>();
            foreach (string fx in fxNames)
            {
                moniker = GetFxMoniker(fx);
                if(moniker.Equals(priorityFxVersion))
                {
                    break;
                }
            }

            return moniker;
        }

        private bool IsDataPlaneProject(Project msBuildProject)
        {
            bool isDataPlane = false;
            if(msBuildProject.FullPath.ToLower().Contains("dataplane"))
            {
                isDataPlane = true;
                string packageId = msBuildProject.GetPropertyValue("PackageId");
                if (packageId.ToLower().Contains("management"))
                {
                    isDataPlane = false;
                }
            }

            return isDataPlane;
        }

        private string GetTargetFullPath(Project sdkProj, string targetFxMoniker)
        {
            string projOutputPath = sdkProj.GetPropertyValue("OutputPath");
            string outputType = sdkProj.GetPropertyValue("OutputType");
            string asmName = sdkProj.GetPropertyValue("AssemblyName");
            string projDirPath = Path.GetDirectoryName(sdkProj.FullPath);
            string fullTargetPath = string.Empty;

            if (outputType.Equals("Library", StringComparison.OrdinalIgnoreCase))
            {
                fullTargetPath = Path.Combine(projDirPath, projOutputPath, targetFxMoniker, String.Concat(asmName, ".dll"));
            }

            return fullTargetPath;
        }
        
        public string GetFxMonikerString(TargetFrameworkMoniker fxMoniker)
        {
            string monikerString = string.Empty;
            switch (fxMoniker)
            {
                case TargetFrameworkMoniker.netcoreapp11:
                    monikerString = "netcoreapp1.1";
                    break;

                case TargetFrameworkMoniker.netcoreapp20:
                    monikerString = "netcoreapp2.0";
                    break;

                case TargetFrameworkMoniker.netstandard14:
                    monikerString = "netstandard1.4";
                    break;

                case TargetFrameworkMoniker.netstandard16:
                    monikerString = "netstandard1.6";
                    break;

                case TargetFrameworkMoniker.net452:
                    monikerString = "net452";
                    break;

                case TargetFrameworkMoniker.net46:
                    monikerString = "net46";
                    break;

                case TargetFrameworkMoniker.net461:
                    monikerString = "net461";
                    break;

                case TargetFrameworkMoniker.net462:
                    monikerString = "net462";
                    break;
            }

            return monikerString;
        }

        public bool IsExpectedFxCategory(TargetFrameworkMoniker targetFxMoniker, TargetFxCategory expectedFxCategory)
        {
            bool expectedFxCat = false;
            switch (targetFxMoniker)
            {
                case TargetFrameworkMoniker.net452:
                case TargetFrameworkMoniker.net46:
                case TargetFrameworkMoniker.net461:
                case TargetFrameworkMoniker.net462:
                    expectedFxCat = (expectedFxCategory == TargetFxCategory.FullDesktop);
                    break;

                case TargetFrameworkMoniker.netcoreapp11:
                case TargetFrameworkMoniker.netcoreapp20:
                case TargetFrameworkMoniker.netstandard14:
                case TargetFrameworkMoniker.netstandard16:
                    expectedFxCat = (expectedFxCategory == TargetFxCategory.NetCore);
                    break;
            }

            return expectedFxCat;
        }

        public SdkProjectType GetProjectType(Project msbuildProj)
        {
            SdkProjectType pType = SdkProjectType.Sdk;
            ICollection<ProjectItem> pkgs = msbuildProj.GetItemsIgnoringCondition("PackageReference");
            if (pkgs.Any<ProjectItem>())
            {
                var testReference = pkgs.Where<ProjectItem>((p) => p.EvaluatedInclude.Equals("xunit", StringComparison.OrdinalIgnoreCase));
                if (testReference.Any<ProjectItem>())
                {
                    pType = SdkProjectType.Test;
                }
                else
                {
                    pType = SdkProjectType.Sdk;
                }
            }

            return pType;
        }

        private bool IsFxSupported(string fxMoniker)
        {
            string lcMoniker = fxMoniker.ToLower();
            bool fxSupported = false;
            TargetFrameworkMoniker validMoniker = TargetFrameworkMoniker.UnSupported;
            switch (lcMoniker)
            {
                case "netcoreapp1.1":
                    validMoniker = TargetFrameworkMoniker.netcoreapp11;
                    fxSupported = true;
                    break;

                case "netcoreapp2.0":
                    validMoniker = TargetFrameworkMoniker.netcoreapp20;
                    fxSupported = true;
                    break;

                case "netstandard1.4":
                    validMoniker = TargetFrameworkMoniker.netstandard14;
                    fxSupported = true;
                    break;

                case "netstandard1.6":
                    validMoniker = TargetFrameworkMoniker.netstandard16;
                    fxSupported = false;
                    break;

                case "net452":
                    validMoniker = TargetFrameworkMoniker.net452;
                    fxSupported = true;
                    break;

                case "net46":
                    validMoniker = TargetFrameworkMoniker.net46;
                    fxSupported = false;
                    break;

                case "net461":
                    validMoniker = TargetFrameworkMoniker.net461;
                    fxSupported = true;
                    break;

                case "net462":
                    validMoniker = TargetFrameworkMoniker.net462;
                    fxSupported = false;
                    break;

                default:
                    validMoniker = TargetFrameworkMoniker.UnSupported;
                    fxSupported = false;
                    break;
            }

            //targetFx = validMoniker;
            return fxSupported;
        }

        private TargetFrameworkMoniker GetFxMoniker(string fx)
        {
            string lcMoniker = fx.ToLower();
            TargetFrameworkMoniker validMoniker = TargetFrameworkMoniker.UnSupported;
            switch (lcMoniker)
            {
                case "netcoreapp1.1":
                    validMoniker = TargetFrameworkMoniker.netcoreapp11;
                    break;

                case "netcoreapp2.0":
                    validMoniker = TargetFrameworkMoniker.netcoreapp20;
                    break;

                case "netstandard1.4":
                    validMoniker = TargetFrameworkMoniker.netstandard14;
                    break;

                case "netstandard1.6":
                    validMoniker = TargetFrameworkMoniker.netstandard16;
                    break;

                case "net452":
                    validMoniker = TargetFrameworkMoniker.net452;
                    break;

                case "net46":
                    validMoniker = TargetFrameworkMoniker.net46;
                    break;

                case "net461":
                    validMoniker = TargetFrameworkMoniker.net461;
                    break;

                case "net462":
                    validMoniker = TargetFrameworkMoniker.net462;
                    break;

                default:
                    validMoniker = TargetFrameworkMoniker.UnSupported;
                    break;
            }

            return validMoniker;
        }

    }

    /*public enum TargetFrameworkMoniker
    {
        net45,
        net452,
        net46,
        net461,
        net462,
        netcoreapp11,
        netcoreapp20,
        netstandard14,
        netstandard16,
        UnSupported
    }

    public enum TargetFxCategory
    {
        FullDesktop,
        NetCore
    }

    public enum SdkProjectType
    {
        Sdk,
        Test,
        ToolsProject,
        Unknown
    }
    */
}
