---
layout: post
tags: rust embedded arm
slug: rust-embedded-setup-part-0
title: "Rust Embedded: Setup - Part 0"
description: Setup for programming the ST Discovery Board 3 with Rust
---

In this post I want show what you have to do, to get started programming a STM32F3 Discovery board with Rust on Windows 10.

We need a few things:

1. The evaluation board: [STM32F3Discovery](https://www.st.com/en/evaluation-tools/stm32f3discovery.html) board with an USB cable.
2. The driver for the STLink/V2: [ST-LINK](https://www.st.com/en/development-tools/stsw-link009.html)
3. On-Chip programming and debugging: [OpenOCD](https://github.com/openocd-org/openocd/releases/download/v0.12.0/openocd-v0.12.0-i686-w64-mingw32.tar.gz)
4. [Rust](https://www.rust-lang.org/learn/get-started)
5. [ARM GNU Toolchain](https://developer.arm.com/downloads/-/arm-gnu-toolchain-downloads): Windows (mingw-w64-i686) hosted cross toolchains - AArch32 bare-metal target (arm-none-eabi)

You don't have to use the F3 Discovery board as it is - as far as I know - already outdated. You probably could also use a new ST board, like one of the Nucleo boards. However, you then would need to adapt the code and crates to the specific board.

After you have installed `Rust` you need to run this commands:

```sh
rustup target add thumbv7em-none-eabi

cargo install cargo-binutils

rustup component add llvm-tools-preview
```

For a quick test, plug the board into your PC via the USB cable. Open a terminal and run:

```sh
openocd -f board/stm32f3discovery.cfg

# Output should look like this:
# xPack Open On-Chip Debugger 0.12.0-01004-g9ea7f3d64-dirty (2023-01-30-15:04)
# Licensed under GNU GPL v2
# For bug reports, read
#         http://openocd.org/doc/doxygen/bugs.html
# Info : The selected transport took over low-level target control. The results might differ compared to plain JTAG/SWD
# srst_only separate srst_nogate srst_open_drain connect_deassert_srst
#  
# Info : Listening on port 6666 for tcl connections
# Info : Listening on port 4444 for telnet connections
# Info : clock speed 1000 kHz
# Info : STLINK V2J41M27 (API v2) VID:PID 0483:374B
# Info : Target voltage: 2.895529
# Info : [stm32f3x.cpu] Cortex-M4 r0p1 processor detected
# Info : [stm32f3x.cpu] target has 6 breakpoints, 4 watchpoints
# Info : starting gdb server for stm32f3x.cpu on 3333
# Info : Listening on port 3333 for gdb connections
```