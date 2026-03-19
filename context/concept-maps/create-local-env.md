# Create Local Env Concept Map

## create-local-env/ (Main App) → `create-local-env/`

### PowerShell Profile Compiler → `create-local-env/PowershellProfileCompiler.cs`
- Entry point → `Program.cs`
- Backs up existing `$PROFILE`, compiles templates into new profile
- Template placeholders: `[USER]`, `[DISK]`, `[EXPLORER]`, `[CODE-EDITOR]`, `[android-studio-exe]`, `{{{utils}}}`, `[workspace-setup]`
- Variable templates → `resources/vars/` (dropbox.txt, git.txt, projects.txt)
- Tool templates → `resources/tools/` (windows-functions.txt, windows-git-simple.txt, windows-git-currbranch.txt)
- Configuration → `appsettings.json`

### Installs Downloader (disabled) → `create-local-env/InstallsDownloader.cs`
- Downloads dev tools from URLs in → `resources/installs/windows.json`
- Target tools: Git, Sublime, VS Code, Visual Studio, Python, PyCharm

### Resource: Git Simple → `create-local-env/resources/tools/windows-git-simple.txt`
- Aliases: `gs` (status), `ga` (add -A), `gc` (commit), `gb` (branch)
- Push/pull: `gpush`, `gpull`
- Branching: `gcheckout`, `gcheckoutb`
- Init: `gi`

### Resource: Git Current Branch → `create-local-env/resources/tools/windows-git-currbranch.txt`
- Full workflow: `gfullcurr [message] [noverify]` — commit + push current branch
- Full specified: `gfull [message] [branch]` — commit + push named branch
- Tags: `gtagcurr [message]` — create + push tag on current branch
- Branch info: `gcurrbranch`, `gbranchtoclip`
- Push/pull current: `gpushcurr`, `gpullcurr`
- Cleanup: `gbranchcleanup [branch]` — delete merged branches
- Checkout by index: `gcheckout [index]`

### Resource: Windows Functions → `create-local-env/resources/tools/windows-functions.txt`
- Process lookup: `getprocbyport [port]`
- Search: `grep [path] [term]` (fake grep for PowerShell)
- Shell: `newpsh` — new PowerShell window
- RDP: `listrdps`, `srdpi [index] [height] [width]`
- Projects: `startsln`, `startslnold`, `startslncd` — solution file navigation
- Workspace: `startworkspace` — launches WorkspaceSetup GUI tool
- IDE launchers: `startpych [folder]`, `startand [folder]`, `startvm [vmname]`
- Clipboard: `clipwd [file]`, `mpwd [file]`
- Terminal: `renametermwindow [name]`
- File ops: `touch [name]`, `open [document]`
- Env vars: `envarlist [searchstring]`

### Resource: Variables → `create-local-env/resources/vars/`
- Dropbox paths → `dropbox.txt` ($dropbox, $dbdocs, $archives, $mng, $knowledge)
- Git clone source → `git.txt` ($gitclonemirceta)
- Project paths → `projects.txt` ($proj_supportv, $proj_bironext)

## Utils/ (CLI Utility) → `Utils/`

### Program (dispatcher) → `Utils/Program.cs`
- `utils envarlist <search>` — search PowerShell profile for env variables
- `utils startsln <subcommand>` — project navigation (delegates to Startsln)

### Startsln → `Utils/Startsln.cs`
- `startsln list` — list saved projects (formatted display)
- `startsln store [name]` — save current directory as project (validates .sln exists)
- `startsln go <key>` — cd to saved project directory
- `startsln pwd <key>` — print saved project path
- `startsln help` — show usage
- Storage file → `projects.txt` (semicolon-delimited key;path pairs)

## startsln-aid/ (Legacy) → `startsln-aid/`
- Older implementation of Startsln (no `pwd` command, different list formatting)
- Superseded by → `Utils/Startsln.cs`

## common/ (Shared Library) → `common/`

### Build → `common/build/Build.cs`
- Runtime detection: configuration, framework (Core/Framework), project/solution paths

### Configuration → `common/configuration/AppSettings.cs`
- Config value retrieval with type conversion

### Conversion → `common/conversion/`
- ColorConverter, DatabaseConverter, DimensionsConverter, HttpConverter, StreamConverter, TypeConverter

### Database → `common/database/`
- Connection, ConnectionString, IConnection — SQL Server ADO.NET with DataSet/DataTable/scalar

### Deployment → `common/deployment/Deployer.cs`
- .NET Core and Framework project publishing via MSBuild

### Encoding → `common/encoding/EncodingUtils.cs`
- Windows-1250 and other code page support (critical for VB6 files)

### IO → `common/io/FileUtils.cs`
- Recursive directory copy

### Logging → `common/logging/`
- Logger, Logger2 — timestamped file and console logging

### Networking → `common/networking/`
- Broadcaster, Http, NetworkingUtils — async HTTP with custom headers

### Processing → `common/processing/ProcessUtils.cs`
- Kill by PID/name, get child processes

### Shell → `common/shell/`
- CommandPrompt.cs — cmd.exe execution
- PowerShell.cs — PowerShell execution
- ShellUtils.cs — unified shell interface (cmd, PowerShell, custom executables)
- Filename.cs — filename utilities

### Serialization → `common/serialization/Serializer.cs`
- JSON via Newtonsoft.Json

### Registry → `common/registry/RegistryUtils.cs`
- Windows registry read/write

### Reflection → `common/reflection/Property.cs`
- Property reflection utilities

### Exceptions → `common/exceptions/MessageCodeException.cs`
- Custom exception with message and code
