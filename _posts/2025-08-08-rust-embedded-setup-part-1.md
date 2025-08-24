---
layout: post
tags: rust embedded arm
slug: rust-embedded-setup-part-1
title: "Rust Embedded: Setup - Part 1"
description: Setup for programming the ST Discovery Board 3 with Rust on Windows 10
---

If you have followed Part 0 of the setup, you are ready to build and flash your first program to the board. For now it is not about the code itself, but how to get it running on the microcontroller. The setup for a very simple program that will light up an on-board LED looks as follows.

_This post was written with the help of ChatGPT. By that I mean, that I let proof read ChatGPT parts of the text, and, in case, used optimizations that ChatGPT offered._

### Code structure

First create a new folder, eg. `basic-led-on`, and then setup the subfolders and files like the following:

```
\-- basic-led-on
    \-- .cargo
        +-- config.toml
    \-- .vscode
        +-- settings.json
    \-- src
        +-- main.rs
    +-- Cargo.toml
    +-- memory.x
    +-- openocd.gdb
```

Let's go over each file and discuss it in more detail.

**.cargo/config.toml**

```toml
[target.thumbv7em-none-eabihf]
runner = "arm-none-eabi-gdb -q -x openocd.gdb"
rustflags = ["-C", "link-arg=-Tlink.x"]

[build]
target = "thumbv7em-none-eabihf"
```

The _config.toml_ file tells Cargo how to build for our microcontroller’s architecture (cross-compiling) and how to run the compiled program. In [Part 0]({% post_url 2024-06-03-rust-embedded-setup-part-0 %}) we installed the `thumbv7em-none-eabihf` [target triple](https://doc.rust-lang.org/cargo/appendix/glossary.html#target), which matches our board. The [build] section sets this as the default target, so Cargo will always compile for it. Under [target.thumbv7em-none-eabihf], we configure two things:
- `runner`: the command Cargo runs when you do `cargo run`. Here it launches `arm-none-eabi-gdb` with a script that flashes the program to the board. `arm-none-eabi-gdb` was installed with the ARM GNU Toolchain from part 0.
- `rustflags`: extra options for the compiler, in this case telling the linker to use our _link.x_ script to place code and data correctly in memory.

The _link.x_ file is usually provided by the **embedded support crate** you’re using - in our case by the [cortex-m-rt](https://docs.rs/cortex-m-rt/latest/cortex_m_rt/){:target="\_blank" rel="noopener noreferrer"} crate.

Here’s how it works:

* The **cortex-m-rt** crate ships with a [linker script template](https://github.com/rust-embedded/cortex-m/blob/master/cortex-m-rt/link.x.in){:target="\_blank" rel="noopener noreferrer"}.
* When you build your project, Cargo automatically copies that template into your build directory and renames it _link.x_.
* The script describes how the compiled program should be laid out in memory: where to put `.text` (your code), `.data` (initialized variables), `.bss` (zeroed variables), stack, and interrupt vectors.
* The **cortex-m-rt** crate expects the user to provide a [memory.x](https://docs.rs/cortex-m-rt/latest/cortex_m_rt/#memoryx){:target="\_blank" rel="noopener noreferrer"} file with the memory layout for your microcontroller. **cortex-m-rt**’s [link.x](https://github.com/rust-embedded/cortex-m/blob/master/cortex-m-rt/link.x.in#L23) will then include it.

**.vscode/settings.json**

```json
{
  "rust-analyzer.check.allTargets": false,
  "rust-analyzer.cargo.target": "thumbv7em-none-eabihf"
}
```

You probably want to install [rust-analyzer](https://rust-analyzer.github.io/){:target="\_blank" rel="noopener noreferrer"} if you are using VSCode and add this `settings.json` file. Here’s what those lines do:

* `"rust-analyzer.check.allTargets": false`: Prevents _rust-analyzer_ from trying to check **every possible target** in your Cargo project (which would include desktop builds). This avoids false errors since standard libraries for embedded targets are different ans our target does not have `std` support.

* `"rust-analyzer.cargo.target": "thumbv7em-none-eabihf"`: Ensures _rust-analyzer_ runs `cargo check` and related tasks using your Cortex-M target triple, so IntelliSense matches what you’ll actually build/flash.


**src/main.rs**

```rust
#![no_main]
#![no_std]

extern crate panic_halt;

use cortex_m_rt::entry;
use cortex_m_semihosting::{self, hprintln};
use stm32f3::stm32f303;

#[entry]
fn main() -> ! {
    // You should see that in your openocd output
    hprintln!("Hello from Discovery");

    let peripherals = &stm32f303::Peripherals::take().unwrap();

    let rcc = &peripherals.RCC;
    rcc.ahbenr.write(|w| w.iopeen().enabled());

    let gpioe = &peripherals.GPIOE;
    gpioe.moder.write(|w| w.moder9().output());
    gpioe.odr.write(|w| w.odr9().set_bit());

    loop {}
}
```

The actual program. Shortly, it takes the _peripherals_, enables GPIO Port E, sets the pin 9 of port E as _output_ and sets the pin to _high_ so that the LED [(User LED3 as documented in the User Manual of the discovery board)](https://www.st.com/resource/en/user_manual/um1570-discovery-kit-with-stm32f303vc-mcu-stmicroelectronics.pdf){:target="\_blank" rel="noopener noreferrer"}, which is attached to this pin, lights up. 

![LEDs screen shot of the user manual](/assets/images/posts/re-setup-part-1/stm32f3_discovery_user_manual_pins.png)
*STM32F3 Discovery Board User Manual p. 19*

**Cargo.toml**

```toml
[package]
name = "basic-led-on"
version = "0.1.0"
edition = "2024"

[dependencies]
stm32f3 = { version = "0.15.1", features = ["stm32f303"] }
cortex-m = "0.7.0"
cortex-m-rt = "0.7.3"
cortex-m-semihosting = "0.5.0"
panic-halt = "0.2.0"
```

Nothing special here. In the _Cargo.toml_ file we define the dependencies we need. See more keys and their definitions at [doc.rust-lang.org](https://doc.rust-lang.org/cargo/reference/manifest.html){:target="\_blank" rel="noopener noreferrer"}.

**memory.x**

```
MEMORY
{
  FLASH : ORIGIN = 0x08000000, LENGTH = 256K
  RAM : ORIGIN = 0x20000000, LENGTH = 40K
  CCRAM : ORIGIN = 0x10000000, LENGTH = 8K
}
```

As already mentioned under the **.cargo/cargo.toml** file, we need to provide a **memory.x** file for the linker. Here we define the exact memory layout for our microcontroller. But where can we find those addresses and sizes? For that we have to look at the [Reference Manual](https://www.st.com/resource/en/reference_manual/rm0316-stm32f303xbcde-stm32f303x68-stm32f328x8-stm32f358xc-stm32f398xe-advanced-armbased-mcus-stmicroelectronics.pdf){:target="\_blank" rel="noopener noreferrer"}. For the RAM we can find the address and length under **3.2.2 Memory map and register boundary addresses** on page 56. Same for the CCRAM. There is documented that the RAM (SRAM) starts at _0x20000000_ and is _40 K_ in size. CCRAM (CCS SRAM) starts at _0x10000000_ and is _8 K_ in size. On the next page, 57, we can find the start address for the main flash memory, FLASH, which starts at _0x08000000_ and is _256 K_ in size.

![RAM layout of the reference manual](/assets/images/posts/re-setup-part-1/stm32f3_reference_manual_ram.png)
*STM32F3 Reference Manual p. 56*

**openocd.gdb**

```
target extended-remote :3333

# print demangled symbols
set print asm-demangle on

# set backtrace limit to not have infinite backtrace loops
set backtrace limit 32

# detect unhandled exceptions, hard faults and panics
break DefaultHandler
break HardFault
break rust_begin_unwind

# *try* to stop at the user entry point (it might be gone due to inlining)
break main

monitor arm semihosting enable

load

# start the process but immediately halt the processor
stepi
```
