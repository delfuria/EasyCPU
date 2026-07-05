# EasyCPU

A comprehensive, cross-platform IDE for teaching Assembly language programming and X86 processor architecture fundamentals.

EasyCPU is an educational tool designed to make learning assembly language and CPU architecture accessible and intuitive. It provides a simplified but functional virtual CPU that implements a subset of X86 instructions, allowing students to write, execute, and debug assembly programs with immediate visual feedback on CPU state.

## 📑 Table of Contents

- [Key Features](#-key-features)
- [What is EasyCPU?](#-what-is-easycpu)
- [What You Can Do](#-what-you-can-do-with-easycpu)
- [Assembly Instruction Set](#-assembly-instruction-set)
- [IDE Overview](#-ide-overview)
- [Documentation](#-documentation)
- [Project Structure](#-project-structure)
- [Requirements](#-requirements)
- [Supported Platforms](#-supported-platforms)
- [Architecture](#-architecture)
- [Build](#-build)
- [Credits](#-credits)
- [License](#-license)

---

## 🎯 Key Features

### Core Capabilities
- **Interactive Assembly Editor** – Write assembly code with syntax support for EasyCPU's instruction set
- **Step-by-Step Debugging** – Execute programs one instruction at a time to understand control flow and side effects
- **Real-Time CPU State Visualization** – Monitor registers, memory, stack, and flags as code executes
- **Integrated Compiler** – Parse and compile assembly code with detailed syntax error reporting
- **Multiple Data Format Viewers** – Display memory, stack, and register values in decimal, hexadecimal, or ASCII
- **Infinite Loop Detection** – Safely interrupt runaway programs with configurable thresholds
- **Code & Data Separation** – Organize assembly into code and data sections with a simple `.DATA` directive

### Educational Features
- **Live Register & Flag Tracking** – Watch how each instruction modifies CPU state
- **Memory Inspection** – View arbitrary memory locations and stack contents
- **Run-to-Instruction** – Execute all instructions up to a selected line for faster iteration
- **Execution State Indicators** – Visual feedback on whether program is running, paused, or stopped
- **Error Highlighting** – Clicking compilation errors jumps directly to problematic code

---

## 📚 What is EasyCPU?

EasyCPU is:
- ✅ An **educational tool** for learning assembly fundamentals
- ✅ A **simplified X86 simulator** with a reduced, easy-to-understand instruction set
- ✅ An **interactive debugger** for real-time program state inspection
- ✅ A **didactic environment** designed for classroom and self-study use

EasyCPU is **not**:
- ❌ A production assembler – it does not generate native machine code for any real platform
- ❌ A full X86 emulator – it implements a simplified subset of instructions and addressing modes
- ❌ Compatible with standard Intel syntax or POSIX assembly conventions
- ❌ Intended for real-world assembly development

---

## 🔧 What You Can Do With EasyCPU

### Write Assembly Programs
Create programs using EasyCPU's instruction set. Programs can:
- Perform arithmetic and logical operations
- Manipulate memory and stack
- Use conditional and unconditional jumps
- Call subroutines
- Work with registers and flags

### Debug Step-by-Step
Execute one instruction at a time to:
- Trace control flow
- Inspect register values after each operation
- Watch memory and stack changes
- Understand how different addressing modes work
- Verify algorithm correctness

### Learn CPU Fundamentals
Develop practical understanding of:
- Register-based computation
- Memory and addressing
- Stack frame management
- Flag-based conditional logic
- Procedure calls and returns
- Infinite loop detection

---

## 📖 Assembly Instruction Set

EasyCPU implements 24 core assembly instructions covering arithmetic, logic, memory access, control flow, and I/O operations:

**Arithmetic:** ADD, SUB, MUL, DIV, INC, DEC, NEG  
**Logic:** AND, OR, XOR, NOT  
**Data Transfer:** MOV, MOVS  
**Comparison:** CMP  
**Conditional Jumps:** JE, JNE, JL, JLE, JG, JGE, JO, JNO, JS, JNS  
**Unconditional Control:** JMP, JCXZ  
**Procedure Calls:** CALL, RET  
**Miscellaneous:** NOP

For complete instruction documentation, register definitions, addressing modes, and flag behavior, see the [**Easy CPU Assembly Reference**](./Docs/Easy%20CPU%20%20Assembly%20Reference.md).

---

## 🎓 IDE Overview

The EasyCPU IDE is organized into four main areas:

### Code & Data Editor
Side-by-side editors for assembly code and data section initialization. Supports syntax highlighting and automatic indentation.

### Register Viewer
Displays all CPU registers (AX, BX, CX, DX, SI, DI, BP, SP) with values in decimal or hexadecimal. Flags are shown separately in color (green for 0, red for 1).

### Memory Inspector
Shows arbitrary memory locations and the stack. Toggle between decimal, hexadecimal, and ASCII formats; view stack in one or two columns.

### Execution Controls
Toolbar buttons and menu commands for:
- **Run** – Execute until program end or infinite loop detection
- **Step** (F10) – Execute one instruction
- **Run to Instruction** (F4) – Execute until selected line
- **Stop** (Shift+F5) – Halt execution
- **New/Open/Save** – File management

### Compilation & Error Reporting
Automatic compilation before execution. Syntax errors are listed with line numbers and descriptions; click an error to navigate directly to it in the editor.

For detailed step-by-step tutorials and screenshots, see the [**EasyCPU IDE Tutorial**](./Docs/EasyCPU%20%20IDE%20Tutorial.md).
*(Note: Tutorial focuses on Windows UI; layouts differ slightly on other platforms.)*

---

## 📝 Documentation

Complete documentation is available in the `Docs/` folder:

- **[Easy CPU Assembly Reference](./Docs/Easy%20CPU%20%20Assembly%20Reference.md)** – Complete instruction set documentation with syntax, examples, and flag behavior
- **[EasyCPU IDE Tutorial](./Docs/EasyCPU%20%20IDE%20Tutorial.md)** – Step-by-step guide to using the IDE, debugging, and managing programs
- **[Toolbar Icons Reference](./ICONE-TOOLBAR.md)** – Visual guide to IDE toolbar buttons

Additional examples and subroutine patterns are available in `Docs/Subroutines/`.

---

## 🚀 Project Structure

```
EasyCPU/
├── EasyCpu.Assembler/          # Assembly parser and compiler
│   ├── Parsing/                # Lexer, parser, AST
│   ├── Processore/             # Virtual CPU and instruction execution
│   └── Memoria/                # Memory and register management
├── EasyCpu.Backend/            # Shared backend logic
├── EasyCpu.Common/             # Common types and utilities
├── EasyCPU/                    # Shared Avalonia UI (views/viewmodels)
├── EasyCPU.Desktop/            # Desktop platform (Windows/macOS/Linux)
├── EasyCPU.Browser/            # Browser platform (WASM)
├── EasyCPU.iOS/                # iOS platform
├── EasyCPU.Android/            # Android platform
├── EasyCpu.Assembler.Tests/    # Unit tests for assembler
└── Docs/                       # Documentation
    ├── Easy CPU Assembly Reference.md
    ├── EasyCPU IDE Tutorial.md
    └── Subroutines/            # Subroutine examples and patterns
```

---

## 🛠️ Requirements

- **.NET SDK 10** or later
- **AvaloniaUI 12.0+** (included via NuGet)
- Platform-specific requirements:
  - **Desktop:** Windows 10+, macOS 10.13+, or Linux (GTK 3.0+)
  - **Browser:** Modern web browser with WebAssembly support
  - **iOS:** iOS 12.0+ with Xcode
  - **Android:** Android 5.0+ (API level 21+) with Android SDK

### Install Required Workloads

To build for specific platforms, install the necessary .NET workloads:

```bash
# Desktop support (Windows/macOS/Linux)
dotnet workload install desktop

# Browser/WebAssembly support
dotnet workload install wasm-tools

# iOS support
dotnet workload install ios

# Android support
dotnet workload install android

# Install all at once
dotnet workload install desktop wasm-tools ios android
```

After installing workloads, restore NuGet dependencies:

```bash
dotnet restore
```

---

## 🖥️ Supported Platforms

EasyCPU is implemented for multiple platforms with a shared core:

| Platform | Status | Features |
|----------|--------|----------|
| **Desktop** (Windows/macOS/Linux) | ✅ Implemented | Full IDE with all features |
| **Browser** (WebAssembly) | ✅ Implemented | Complete IDE running in browser via WASM |
| **iOS** | ✅ Implemented | Touch-optimized interface for iPad/iPhone |
| **Android** | ✅ Implemented | Native Android app interface |

All platforms share the same assembly compiler and virtual CPU core, ensuring consistent behavior across devices.

---

## 🏗️ Architecture

EasyCPU is built on a modular architecture with clear separation of concerns:

### Core Components

- **EasyCpu.Assembler** – Assembly language parser and compiler; translates assembly source to internal instruction format
- **EasyCpu.Backend** – Virtual CPU implementation; simulates X86-subset processor with registers, memory, stack, and instruction execution
- **EasyCpu.Common** – Shared data structures and utilities used across projects
- **EasyCPU.* (UI Projects)** – Platform-specific front-ends (Desktop, Browser, iOS, Android)

### Technology Stack

- **Framework:** .NET SDK 10
- **UI Framework:** [AvaloniaUI](https://avaloniaui.net/) – Cross-platform, XAML-based UI for Desktop, Browser, iOS, and Android
- **Language:** C#
- **Browser Target:** WebAssembly (Emscripten compilation)

---

## 🔨 Build

### Build for Desktop (Windows/macOS/Linux)

```bash
# Restore dependencies
dotnet restore EasyCPU.Desktop

# Build
dotnet build EasyCPU.Desktop -c Release

# Or directly run
dotnet run --project EasyCPU.Desktop -c Release
```

### Build for Browser (WebAssembly)

```bash
# Restore dependencies
dotnet restore EasyCPU.Browser

# Publish for WebAssembly (creates wwwroot output)
dotnet publish EasyCPU.Browser -c Release

# The output will be in EasyCPU.Browser/bin/Release/net10.0/publish/wwwroot
```

### Build for iOS

```bash
# Restore dependencies
dotnet restore EasyCPU.iOS

# Build
dotnet build EasyCPU.iOS -c Release -f net10.0-ios

# Or create an IPA for deployment
dotnet publish EasyCPU.iOS -c Release -f net10.0-ios
```

### Build for Android

```bash
# Restore dependencies
dotnet restore EasyCPU.Android

# Build
dotnet build EasyCPU.Android -c Release -f net10.0-android

# Or create an APK for deployment
dotnet publish EasyCPU.Android -c Release -f net10.0-android
```

### Clean Build

To perform a clean rebuild across all projects:

```bash
# Clean all build artifacts
dotnet clean

# Restore workloads and dependencies
dotnet workload restore
dotnet restore

# Rebuild all projects
dotnet build -c Release
```

---

## 👥 Credits

EasyCPU was developed by **Paolo Meozzi** and **Stefano Del Furia**.

<div align="center">
    <a href="https://www.jetbrains.com/?from=EasyCpu">
        <img src="https://raw.githubusercontent.com/delfuria/EasyCPU/main/images/jetbrains.svg" alt="JetBrains" width="96">
    </a>
    <br/>
    <p><strong>Special thanks to <a href="https://www.jetbrains.com/?from=EasyCpu">JetBrains</a></strong> for supporting this project with open-source licenses for their IDEs.</p>
</div>

<div align="center">
    <a href="https://avaloniaui.net/">
        <img src="https://raw.githubusercontent.com/delfuria/EasyCPU/main/images/avalonia.svg" alt="Avalonia" width="96">
    </a>
    <br/>
    <p><strong>Special thanks to <a href="https://avaloniaui.net/">Avalonia</a></strong> for providing the cross-platform UI framework that powers EasyCPU across Desktop, Browser, iOS, and Android.</p>
</div>

---

## 📄 License

See repository for license details.
