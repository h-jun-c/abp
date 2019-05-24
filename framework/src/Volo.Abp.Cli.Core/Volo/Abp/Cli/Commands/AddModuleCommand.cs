﻿using System;
using System.IO;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using System.Threading.Tasks;
using Volo.Abp.Cli.Args;
using Volo.Abp.Cli.ProjectModification;
using Volo.Abp.Cli.Utils;
using Volo.Abp.DependencyInjection;

namespace Volo.Abp.Cli.Commands
{
    public class AddModuleCommand : IConsoleCommand, ITransientDependency
    {
        public ILogger<AddModuleCommand> Logger { get; set; }

        protected SolutionModuleAdder SolutionModuleAdder { get; }

        public AddModuleCommand(SolutionModuleAdder solutionModuleAdder)
        {
            SolutionModuleAdder = solutionModuleAdder;
            Logger = NullLogger<AddModuleCommand>.Instance;
        }

        public async Task ExecuteAsync(CommandLineArgs commandLineArgs)
        {
            if (commandLineArgs.Target == null)
            {
                throw new CliUsageException("Module name is missing!" + Environment.NewLine + Environment.NewLine + GetUsageInfo());
            }

            var skipDbMigrations = Convert.ToBoolean(
                commandLineArgs.Options.GetOrNull(Options.DbMigrations.Skip) ?? "false");

            await SolutionModuleAdder.AddAsync(
                GetSolutionFile(commandLineArgs),
                commandLineArgs.Target,
                skipDbMigrations
            );
        }

        protected virtual string GetSolutionFile(CommandLineArgs commandLineArgs)
        {
            var providedSolutionFile = PathHelper.NormalizePath(
                commandLineArgs.Options.GetOrNull(
                    AddPackageCommand.Options.Project.Short,
                    AddPackageCommand.Options.Project.Long
                )
            );

            if (!providedSolutionFile.IsNullOrWhiteSpace())
            {
                return providedSolutionFile;
            }

            var foundSolutionFiles = Directory.GetFiles(Directory.GetCurrentDirectory(), "*.sln");
            if (foundSolutionFiles.Length == 1)
            {
                return foundSolutionFiles[0];
            }

            if (foundSolutionFiles.Length == 0)
            {
                throw new CliUsageException("'abp add-module' command should be used inside a folder contaning a .sln file!");
            }

            //foundSolutionFiles.Length > 1

            var sb = new StringBuilder("There are multiple solution (.sln) files in the current directory. Please specify one of the files below:");

            foreach (var foundSolutionFile in foundSolutionFiles)
            {
                sb.AppendLine("* " + foundSolutionFile);
            }

            sb.AppendLine("Example:");
            sb.AppendLine($"abp add-module {commandLineArgs.Target} -p {foundSolutionFiles[0]}");

            throw new CliUsageException(sb.ToString());
        }

        protected virtual string GetUsageInfo()
        {
            var sb = new StringBuilder();

            sb.AppendLine("");
            sb.AppendLine("'add-module' command is used to add a multi-package ABP module to a solution.");
            sb.AppendLine("It should be used in a folder containing a .sln file.");
            sb.AppendLine("");
            sb.AppendLine("Usage:");
            sb.AppendLine("  abp add-module <module-name> [-s|--solution]");
            sb.AppendLine("");
            sb.AppendLine("Options:");
            sb.AppendLine("  -s|--solution <solution-file>    Specify the solution file explicitly.");
            sb.AppendLine("  --skip-db-migrations <boolean>    Specify if a new migration will be added or not.");
            sb.AppendLine("");
            sb.AppendLine("Examples:");
            sb.AppendLine("  abp add-module Volo.Blogging                      Adds the module to the current soluton.");
            sb.AppendLine("  abp add-module Volo.Blogging -s Acme.BookStore    Adds the module to the given soluton.");
            sb.AppendLine("  abp add-module Volo.Blogging -s Acme.BookStore --skip-db-migrations false    Adds the module to the given soluton but doesn't add-migration.");
            sb.AppendLine("");

            return sb.ToString();
        }

        public static class Options
        {
            public static class Solution
            {
                public const string Short = "s";
                public const string Long = "solution";
            }

            public static class DbMigrations
            {
                public const string Skip = "skip-db-migrations";
            }
        }
    }
}