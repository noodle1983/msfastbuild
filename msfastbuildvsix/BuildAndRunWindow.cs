//------------------------------------------------------------------------------
// <copyright file="BuildAndRunWindow.cs" company="Company">
//     Copyright (c) Company.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

namespace msfastbuildvsix
{
    using System;
    using System.Runtime.InteropServices;
    using Microsoft.VisualStudio.Shell;
    using System.Windows.Controls;
    using System.Windows;
    using System.Drawing;
    using System.Collections.Generic;
    using Microsoft.VisualStudio.VCProjectEngine;
    using EnvDTE;
    using Newtonsoft.Json;
    using System.IO;

    [Serializable]
    public struct DebugInstanceInfo
    {
        public string projectName;
        public string cmdParam;
        public string cmdDir;

    }

    [Serializable]
    public struct ConfigStruct
    {
        public Dictionary<string, List<DebugInstanceInfo>> allDebugInstance;
    }

    /// <summary>
    /// This class implements the tool window exposed by this package and hosts a user control.
    /// </summary>
    /// <remarks>
    /// In Visual Studio tool windows are composed of a frame (implemented by the shell) and a pane,
    /// usually implemented by the package implementer.
    /// <para>
    /// This class derives from the ToolWindowPane class provided from the MPF in order to use its
    /// implementation of the IVsUIElementPane interface.
    /// </para>
    /// </remarks>
    [Guid("52b8741c-7f01-4309-826e-0a2db0b2b298")]
    public class BuildAndRunWindow : ToolWindowPane
    {
        public static Dictionary<string, List<DebugInstanceInfo>> allDebugInstance = new Dictionary<string, List<DebugInstanceInfo>>();


        public static string GetSlnFullPathName()
        {
            var package = FASTBuild.Instance.Package;
            if (null == package || null == package.m_dte.Solution) {
                return string.Empty;
            }
            return package.m_dte.Solution.FullName;
        }

        public static string GetSlnName()
        {
            var fullName = GetSlnFullPathName();
            if (string.IsNullOrEmpty(fullName)) { return string.Empty; }
            return Path.GetFileNameWithoutExtension(fullName);
        }

        public static List<VCProject> GetAllVcProject()
        {
            List<VCProject> allVcProject = new List<VCProject>() ;
            var package = FASTBuild.Instance.Package;
            if (null == package || null == package.m_dte.Solution) {
                return allVcProject;
            }
            var sln = package.m_dte.Solution;

            var projects = sln.Projects;
            foreach(Project proj in projects)
            {
                if (proj == null) { continue; }
                var vcproj = proj.Object as VCProject;
                if (vcproj == null) { continue; }
                if (vcproj.ActiveConfiguration.ConfigurationType == ConfigurationTypes.typeApplication)
                {
                    allVcProject.Add(vcproj);
                }

            }

            return allVcProject;

        }

        public static string GetConfigPath()
        {
            var package = FASTBuild.Instance.Package;
            if (null == package || null == package.m_dte.Solution)
            {
                return string.Empty;
            }

            string slnPath = package.m_dte.Solution.FullName;

            string dir = Path.GetDirectoryName(slnPath);
            string slnName = Path.GetFileNameWithoutExtension(slnPath);
            return dir + "\\" + slnName + ".debuginstance.json";
            
        }

        public static void WriteCofnigFile()
        {
            var configPath = GetConfigPath();
            ConfigStruct config;
            config.allDebugInstance = allDebugInstance;
            string fileContent = JsonConvert.SerializeObject(config);
            File.WriteAllText(configPath, fileContent);
        }

        public Dictionary<string, List<DebugInstanceInfo>> GetDebugInstanceConfig()
        {
            var configPath = GetConfigPath();
            if (!File.Exists(configPath))
            {
                allDebugInstance = new Dictionary<string, List<DebugInstanceInfo>>();
                var defaultInstance = new List<DebugInstanceInfo>();
                var projects = GetAllVcProject();
                if (projects.Count == 0) { return allDebugInstance; }
                foreach(var proj in projects)
                {
                    DebugInstanceInfo info = new DebugInstanceInfo();
                    info.projectName = proj.Name;
                    defaultInstance.Add(info);
                }
                allDebugInstance["default"] = defaultInstance;

                WriteCofnigFile();
                return allDebugInstance;
            }

            var configStruct = JsonConvert.DeserializeObject<ConfigStruct>(configPath);
            allDebugInstance = configStruct.allDebugInstance;
            return allDebugInstance;
        }
        /// <summary>
        /// Initializes a new instance of the <see cref="BuildAndRunWindow"/> class.
        /// </summary>
        //<grid>
        //    <stackpanel orientation="vertical">
        //        <stackpanel orientation="horizontal">
        //            <textblock width="200" background="white" height ="40" text="192.168.0.1" textalignment="center" fontsize="30" />
        //            <button content="installfastbuild" click="button1_click" width="90" height="40" x:name="installbtn" margin="10,0"/>
        //        </stackpanel>
        //        <stackpanel orientation="horizontal">
        //            <button content="build all" click="button1_click" width="90" height="40" x:name="buildallbtn" margin="0"/>
        //            <button content="clean" click="button1_click" width="90" height="40" x:name="cleanallbtn" margin="10"/>
        //            <button content="run all" click="button1_click" width="90" height="40" x:name="runallbtn" margin="10"/>
        //        </stackpanel>
        //        <scrollviewer>
        //            <stackpanel orientation="horizontal">
        //                <textblock margin="10" width="150" horizontalalignment="left" verticalalignment="center">projectname</textblock>
        //                <button content="run" click="button1_click" width="30" height="30" x:name="runbtn" margin="10"/>
        //                <button content="del" click="button1_click" width="30" height="30" x:name="delbtn" margin="10"/>
        //            </stackpanel>

        //        </scrollviewer>
        //        <textblock margin="10" horizontalalignment="center">buildandrunwindow</textblock>
        //    </stackpanel>
        //</grid>
        public BuildAndRunWindow() : base(null)
        {
            this.Caption = "BuildAndRunWindow";


            GetDebugInstanceConfig();
            CreateContent();
        }

        private void CreateContent()
        {
            // Add Layout control
            var topStackPanel = new StackPanel();
            topStackPanel.Orientation = Orientation.Vertical;
            topStackPanel.HorizontalAlignment = HorizontalAlignment.Left;
            topStackPanel.VerticalAlignment = VerticalAlignment.Top;

            {
                var installStackPanel = new StackPanel();
                installStackPanel.Orientation = Orientation.Horizontal;
                installStackPanel.HorizontalAlignment = HorizontalAlignment.Left;
                installStackPanel.VerticalAlignment = VerticalAlignment.Top;
                topStackPanel.Children.Add(installStackPanel);

                var sharedIpTextBlock = new TextBox();
                sharedIpTextBlock.Margin = new Thickness(10, 0, 0, 0);
                sharedIpTextBlock.Width = 190;
                sharedIpTextBlock.Height = 40;
                sharedIpTextBlock.FontSize = 30;
                sharedIpTextBlock.TextAlignment = TextAlignment.Center;
                sharedIpTextBlock.Text = "192.168.0.1";
                installStackPanel.Children.Add(sharedIpTextBlock);

                var installButton = new Button();
                installButton.Width = 90;
                installButton.Height = 40;
                installButton.Margin = new Thickness(10, 0, 0, 0);
                installButton.Content = "InstallFastbuild";
                installButton.Name = "installbtn";
                installButton.Click += new RoutedEventHandler(editCurProject);
                installStackPanel.Children.Add(installButton);
            }

            {
                var globalBtnStackPanel = new StackPanel();
                globalBtnStackPanel.Orientation = Orientation.Horizontal;
                globalBtnStackPanel.HorizontalAlignment = HorizontalAlignment.Left;
                globalBtnStackPanel.VerticalAlignment = VerticalAlignment.Top;
                topStackPanel.Children.Add(globalBtnStackPanel);

                var buildAllButton = new Button();
                buildAllButton.Width = 90;
                buildAllButton.Height = 40;
                buildAllButton.Margin = new Thickness(10, 20, 0, 10);
                buildAllButton.Content = "BuildAll";
                buildAllButton.Name = "buildallbtn";
                buildAllButton.Click += new RoutedEventHandler(BuildAll);
                globalBtnStackPanel.Children.Add(buildAllButton);

                var cleanAllButton = new Button();
                cleanAllButton.Width = 90;
                cleanAllButton.Height = 40;
                cleanAllButton.Margin = new Thickness(10, 20, 0, 10);
                cleanAllButton.Content = "Clean";
                cleanAllButton.Name = "cleanallbtn";
                cleanAllButton.Click += new RoutedEventHandler(CleanAll);
                globalBtnStackPanel.Children.Add(cleanAllButton);

                var runAllButton = new Button();
                runAllButton.Width = 90;
                runAllButton.Height = 40;
                runAllButton.Margin = new Thickness(10, 20, 0, 10);
                runAllButton.Content = "RunAll";
                runAllButton.Name = "runallbtn";
                runAllButton.Click += new RoutedEventHandler(RunAll);
                globalBtnStackPanel.Children.Add(runAllButton);
            }


            {
                // Define a ScrollViewer
                var scrollViewStackPanel = new StackPanel();
                scrollViewStackPanel.Orientation = Orientation.Vertical;
                scrollViewStackPanel.HorizontalAlignment = HorizontalAlignment.Left;
                scrollViewStackPanel.VerticalAlignment = VerticalAlignment.Top;

                var scrollViewer = new ScrollViewer();
                scrollViewer.Margin = new Thickness(10, 0, 0, 10);
                scrollViewer.HorizontalScrollBarVisibility = ScrollBarVisibility.Auto;
                scrollViewer.VerticalScrollBarVisibility = ScrollBarVisibility.Auto;
                scrollViewer.Content = scrollViewStackPanel;
                topStackPanel.Children.Add(scrollViewer);

                for (int i = 0; i < 3; i++)
                {
                    var projStackPanel = new StackPanel();
                    projStackPanel.Orientation = Orientation.Horizontal;
                    projStackPanel.HorizontalAlignment = HorizontalAlignment.Left;
                    projStackPanel.VerticalAlignment = VerticalAlignment.Top;
                    var panelBtn = new Button();
                    panelBtn.Content = projStackPanel;
                    panelBtn.Click += new RoutedEventHandler(showProjParam);
                    scrollViewStackPanel.Children.Add(panelBtn);

                    var projectNameTextBlock = new TextBlock();
                    projectNameTextBlock.Margin = new Thickness(0, 0, 0, 0);
                    projectNameTextBlock.Width = 185;
                    projectNameTextBlock.Height = 30;
                    //projectNameTextBlock.FontSize = 0;
                    projectNameTextBlock.TextAlignment = TextAlignment.Center;
                    projectNameTextBlock.VerticalAlignment = VerticalAlignment.Center;
                    projectNameTextBlock.Text = GetSlnFullPathName(); ;
                    projStackPanel.Children.Add(projectNameTextBlock);

                    var runButton = new Button();
                    runButton.Width = 40;
                    runButton.Height = 30;
                    runButton.Margin = new Thickness(10, 0, 0, 0);
                    runButton.Content = "Run";
                    runButton.Name = "runbtn";
                    runButton.Click += new RoutedEventHandler(runProject);
                    projStackPanel.Children.Add(runButton);

                    var delButton = new Button();
                    delButton.Width = 40;
                    delButton.Height = 30;
                    delButton.Margin = new Thickness(10, 0, 0, 0);
                    delButton.Content = "Del";
                    delButton.Name = "delbtn";
                    delButton.Click += new RoutedEventHandler(delProject);
                    projStackPanel.Children.Add(delButton);
                }
            }

            //project
            {
                var projStackPanel = new StackPanel();
                projStackPanel.Orientation = Orientation.Horizontal;
                projStackPanel.HorizontalAlignment = HorizontalAlignment.Left;
                projStackPanel.VerticalAlignment = VerticalAlignment.Top;
                topStackPanel.Children.Add(projStackPanel);

                TextBlock projLabelBlock = new TextBlock();
                projLabelBlock.TextWrapping = TextWrapping.Wrap;
                projLabelBlock.Margin = new Thickness(10, 0, 0, 10);
                projLabelBlock.Height = 30;
                projLabelBlock.Text = "project:";
                projLabelBlock.Width = 80;
                projLabelBlock.VerticalAlignment = VerticalAlignment.Center;
                projLabelBlock.TextAlignment = TextAlignment.Center;
                projStackPanel.Children.Add(projLabelBlock);

                var projComboBox = new ComboBox();
                projComboBox.IsEditable = false;
                projComboBox.IsReadOnly = false;
                projComboBox.Margin = new Thickness(10, 0, 0, 10);
                projComboBox.Width = 200;
                projComboBox.Height = 30;
                var projs = GetAllVcProject();
                foreach (var proj in projs)
                {
                    var projNameText = new TextBlock();
                    projNameText.Text = proj.Name;
                    projComboBox.Items.Add(projNameText);
                }
                projStackPanel.Children.Add(projComboBox);
            }

            //cmd args
            {
                var runParamStackPanel = new StackPanel();
                runParamStackPanel.Orientation = Orientation.Horizontal;
                runParamStackPanel.HorizontalAlignment = HorizontalAlignment.Left;
                runParamStackPanel.VerticalAlignment = VerticalAlignment.Top;
                topStackPanel.Children.Add(runParamStackPanel);

                TextBlock runParamLabelBlock = new TextBlock();
                runParamLabelBlock.TextWrapping = TextWrapping.Wrap;
                runParamLabelBlock.Margin = new Thickness(10, 0, 0, 10);
                runParamLabelBlock.Width = 80;
                runParamLabelBlock.Height = 30;
                runParamLabelBlock.Text = "run parameter:";
                runParamLabelBlock.TextAlignment = TextAlignment.Center;
                runParamLabelBlock.VerticalAlignment = VerticalAlignment.Center;
                runParamStackPanel.Children.Add(runParamLabelBlock);

                var runParamValueBlock = new TextBox();
                runParamValueBlock.Margin = new Thickness(10, 0, 0, 10);
                runParamValueBlock.Width = 200;
                runParamValueBlock.Height = 30;
                runParamValueBlock.TextAlignment = TextAlignment.Center;
                runParamValueBlock.Text = GetConfigPath() ;
                runParamStackPanel.Children.Add(runParamValueBlock);
            }

            {
                var runDirStackPanel = new StackPanel();
                runDirStackPanel.Orientation = Orientation.Horizontal;
                runDirStackPanel.HorizontalAlignment = HorizontalAlignment.Left;
                runDirStackPanel.VerticalAlignment = VerticalAlignment.Top;
                topStackPanel.Children.Add(runDirStackPanel);

                TextBlock runDirLabelBlock = new TextBlock();
                runDirLabelBlock.TextWrapping = TextWrapping.Wrap;
                runDirLabelBlock.Margin = new Thickness(10, 0, 0, 10);
                runDirLabelBlock.Height = 30;
                runDirLabelBlock.Width = 80;
                runDirLabelBlock.Text = "run directory:";
                runDirLabelBlock.TextAlignment = TextAlignment.Center;
                runDirLabelBlock.VerticalAlignment = VerticalAlignment.Center;
                runDirStackPanel.Children.Add(runDirLabelBlock);

                var runDirValueBlock = new TextBox();
                runDirValueBlock.Margin = new Thickness(10, 0, 0, 10);
                runDirValueBlock.Width = 200;
                runDirValueBlock.Height = 30;
                runDirValueBlock.TextAlignment = TextAlignment.Center;
                runDirValueBlock.Text = GetSlnName();
                runDirStackPanel.Children.Add(runDirValueBlock);
            }


            {
                var btnStackPanel = new StackPanel();
                btnStackPanel.Orientation = Orientation.Horizontal;
                btnStackPanel.HorizontalAlignment = HorizontalAlignment.Left;
                btnStackPanel.VerticalAlignment = VerticalAlignment.Top;
                topStackPanel.Children.Add(btnStackPanel);

                var editButton = new Button();
                editButton.Width = 90;
                editButton.Height = 30;
                editButton.Margin = new Thickness(10, 0, 0, 10);
                editButton.Content = "Save";
                editButton.Name = "editobtn";
                editButton.Click += new RoutedEventHandler(editCurProject);
                btnStackPanel.Children.Add(editButton);

                var addnewButton = new Button();
                addnewButton.Width = 120;
                addnewButton.Height = 30;
                addnewButton.Margin = new Thickness(10, 0, 0, 10);
                addnewButton.Content = "New Instance";
                addnewButton.Name = "addnewobtn";
                addnewButton.Click += new RoutedEventHandler(newRunProject);
                btnStackPanel.Children.Add(addnewButton);

            }

            // Add the StackPanel as the lone child of the ScrollViewer
            // This is the user control hosted by the tool window; Note that, even if this class implements IDisposable,
            // we are not calling Dispose on this object. This is because ToolWindowPane calls Dispose on
            // the object returned by the Content property.
            this.Content = topStackPanel;
        }

        private void newRunProject(object sender, RoutedEventArgs e)
        {
            throw new NotImplementedException();
        }

        private void showProjParam(object sender, RoutedEventArgs e)
        {
            throw new NotImplementedException();
        }

        private void delProject(object sender, RoutedEventArgs e)
        {
            throw new NotImplementedException();
        }

        private void runProject(object sender, RoutedEventArgs e)
        {
            throw new NotImplementedException();
        }

        private void RunAll(object sender, RoutedEventArgs e)
        {
            throw new NotImplementedException();
        }

        private void CleanAll(object sender, RoutedEventArgs e)
        {
            throw new NotImplementedException();
        }

        private void BuildAll(object sender, RoutedEventArgs e)
        {
            throw new NotImplementedException();
        }

        private void editCurProject(object sender, RoutedEventArgs e)
        {
            throw new NotImplementedException();
        }
    }
}
