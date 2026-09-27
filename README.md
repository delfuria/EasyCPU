# EasyCPU

[![Deploy Browser app to GitHub Pages](https://github.com/delfuria/EasyCPU/actions/workflows/deploy-browser.yml/badge.svg)](https://github.com/delfuria/EasyCPU/actions/workflows/deploy-browser.yml)

**🌐 Live demo: [delfuria.github.io/EasyCPU](https://delfuria.github.io/EasyCPU/)** — run EasyCPU directly in your browser, no installation required.

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
- **Integrated Compiler** – Parse and compile assembly code with detailed syntax error reporting; runtime errors (division by zero, stack overflow, invalid IP…) are reported with the offending line
- **Multiple Data Format Viewers** – Display memory, stack, and register values in decimal, hexadecimal, or ASCII
- **Infinite Loop Detection** – Safely interrupt runaway programs with configurable thresholds
- **Code & Data Separation** – Organize assembly into code and data sections; the data section supports MASM-style `DB`/`DW` variables, `EQU` constants, strings, `DUP` and `ORG`
- **Console I/O** – DOS-style `int 21h` services for keyboard input and text output

### Educational Features
- **Live Register & Flag Tracking** – Watch how each instruction modifies CPU state
- **Memory Inspection** – View memory and stack contents, plus a Symbols list with the address, type and current value of each data name
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

EasyCPU implements an 8086-style instruction set (16-bit and 8-bit registers, CF/ZF/SF/OF/DF flags with x86 bit layout, base+index addressing such as `[bx+si+2]`):

**Arithmetic:** ADD, ADC, SUB, SBB, MUL, IMUL, DIV, IDIV, INC, DEC, NEG, CBW, CWD  
**Logic:** AND, OR, XOR, NOT, TEST  
**Shift/Rotate:** SHL, SHR, SAR, ROL, ROR, RCL, RCR  
**Data Transfer:** MOV, XCHG, LEA  
**String:** MOVSB/MOVSW (MOVS), LODSB/LODSW, STOSB/STOSW, CMPSB/CMPSW, SCASB/SCASW with REP, REPE/REPZ, REPNE/REPNZ prefixes  
**Comparison:** CMP  
**Conditional Jumps (signed):** JE/JZ, JNE/JNZ, JL, JLE, JG, JGE, JO, JNO, JS, JNS (plus x86 synonyms JNGE, JNG, JNLE, JNL)  
**Conditional Jumps (unsigned):** JA, JAE, JB, JBE, JC, JNC (plus synonyms JNBE, JNB, JNAE, JNA)  
**Loops:** LOOP, LOOPE/LOOPZ, LOOPNE/LOOPNZ, JCXZ  
**Unconditional Control:** JMP  
**Procedure Calls:** CALL, RET, RET n  
**Stack:** PUSH, POP, PUSHF, POPF  
**Flags:** CLC, STC, CMC, CLD, STD  
**Interrupts:** INT (DOS-style `int 21h` console services)  
**Miscellaneous:** NOP, STOP

MUL/DIV operate on unsigned values, IMUL/IDIV on signed values.

`int 21h` provides DOS-style services selected via `AH`: `01h` read a character with echo (into `AL`), `02h` write the character in `DL`, `07h` read a character without echo, `09h` write the `$`-terminated string at `DX`, `0Ah` read a line into the buffer at `DX`, `4Ch` terminate the program. Output and keyboard input are shown/captured in the dedicated **Console** panel.

The data section supports MASM-style symbolic declarations: `DB`/`DW` variables, `EQU` constants, strings, `DUP`, `ORG` and `offset` (example programs in [`Docs/samples`](./Docs/samples)).

For complete instruction documentation, register definitions, addressing modes, and flag behavior, see the [**Easy CPU Assembly Reference**](./Docs/Easy%20CPU%20%20Assembly%20Reference.md).

---

## 🎓 IDE Overview

The EasyCPU IDE is organized into four main areas:

### Code & Data Editor
Side-by-side editors for assembly code and data section initialization. Supports syntax highlighting and automatic indentation.

### Register Viewer
Displays all CPU registers (AX, BX, CX, DX, SI, DI, BP, SP, IP) in the selected format, with the high and low bytes of AX–DX (`AX = 0141 [AH=01 AL=41]`). The flags are listed as C, Z, S, O, D (carry, zero, sign, overflow, direction).

### Memory Inspector
Shows the data memory and the stack. Toggle between decimal, hexadecimal, and character formats (the choice is remembered); view the stack in one or two columns. Below the memory, the **Symbols** section lists the names defined in the data section.

### Console Panel
Displays output and captures keyboard input for `int 21h` calls. Auto-activates on `int 21h` and shows a blinking cursor while waiting for a keypress.

### Execution Controls
Toolbar buttons and menu commands for:
- **Run** – Execute until program end or infinite loop detection
- **Step** (F10) – Execute one instruction (for `rep`-prefixed string instructions, one repetition per step)
- **Run to Instruction** (F4) – Execute until selected line
- **Stop** (Shift+F5) – Halt execution
- **New/Open/Save** – File management, with the 10 most recent programs (in the browser, under the **Recenti** entry of the side menu)

### Compilation & Error Reporting
Automatic compilation before execution. Syntax errors are listed with line numbers and descriptions; click an error to navigate directly to it in the editor. Errors detected while the program runs are shown in the same panel as *Esecuzione* errors, with the line of the instruction that caused them.

### Settings Persistence
Options, panel layout, recent programs and breakpoints are kept between sessions: in files on the desktop, in the browser's `localStorage` in the web version (clear the site data to reset them).

For detailed step-by-step tutorials and screenshots, see the [**EasyCPU IDE Tutorial**](./Docs/EasyCPU%20%20IDE%20Tutorial.md).
*(Note: Tutorial focuses on Windows UI; layouts differ slightly on other platforms.)*

---

## 📝 Documentation

Complete documentation is available in the `Docs/` folder:

- **[Easy CPU Assembly Reference](./Docs/Easy%20CPU%20%20Assembly%20Reference.md)** – Complete instruction set documentation with syntax, examples, and flag behavior
- **[EasyCPU IDE Tutorial](./Docs/EasyCPU%20%20IDE%20Tutorial.md)** – Step-by-step guide to using the IDE, debugging, and managing programs
- **[Toolbar Icons Reference](./ICONE-TOOLBAR.md)** – Visual guide to IDE toolbar buttons

Ready-to-run example programs (`.asj`) are available in [`Docs/samples/`](./Docs/samples), organized by topic (data transfer, arithmetic and flags, logic and shifts, stack and subroutines, data section, `int 21h` console, complete programs, jumps and loops, string instructions, errors); each one starts with a comment describing what it shows and the expected result. The original examples in the legacy `.as` format are in `Docs/Subroutines/`.

Design documents for the x86 extensions (roadmap, open proposals and their designs) are in [`Proposte/`](./Proposte).

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
├── Docs/                       # Documentation
│   ├── Easy CPU Assembly Reference.md
│   ├── EasyCPU IDE Tutorial.md
│   ├── samples/                # Example programs (.asj) by topic
│   └── Subroutines/            # Original examples (.as)
└── Proposte/                   # x86 extension roadmap and design documents
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
| **Browser** (WebAssembly) | ✅ Implemented | Complete IDE running in browser via WASM; options, layout, recent programs and breakpoints are kept in the browser's `localStorage` (clear the site data to reset them) |
| **iOS** | ✅ Implemented | Touch-optimized interface for iPad/iPhone |
| **Android** | ✅ Implemented | Native Android app interface |

All platforms share the same assembly compiler and virtual CPU core, ensuring consistent behavior across devices.

---

## 🏗️ Architecture

EasyCPU is built on a modular architecture with clear separation of concerns:

### Core Components

- **EasyCpu.Assembler** – Assembly language parser and compiler, and the virtual CPU that executes the compiled program (registers, flags, memory, stack)
- **EasyCpu.Backend** – File formats (`.asj`, legacy `.as`) and settings storage (files on the desktop, `localStorage` in the browser)
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
