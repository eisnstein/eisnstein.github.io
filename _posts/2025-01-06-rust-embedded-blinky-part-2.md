---
layout: post
tags: rust embedded arm
slug: rust-embedded-blinky-part-2
title: "Rust Embedded: Blinky - Part 2"
description: Writing the "Hello World" of embedded programming in Rust.
---

In this part we are going to use a hal crate which abstracts away most of the boilerplate code we had to write in our _raw_ version from Part 1. We are going to use the [`stm32f3` crate](https://docs.rs/stm32f3/0.15.1/stm32f3/){:target="\_blank" rel="noopener noreferrer"}.

Code on [GitHub](https://github.com/eisnstein/rust-embedded-blinky/tree/hal-naive-delay){:target="\_blank" rel="noopener noreferrer"}.

Here is the full code example for our blinky program with the hal crate. Further down we'll go through the code in more detail. 

```rust
#![no_main]
#![no_std]

extern crate panic_halt;

use core::arch::asm;

use cortex_m_rt::entry;
use cortex_m_semihosting::{self, hprintln};
use stm32f3::stm32f303::{self};

#[entry]
fn main() -> ! {
    // You should see that in your openocd output
    hprintln!("Hello from Discovery");

    let peripherals = stm32f303::Peripherals::take().unwrap();

    let rcc = &peripherals.RCC;
    // Set HSI clock on
    rcc.cr.write(|w| w.hsion().set_bit());
    // Enable GPIO Port E clock
    rcc.ahbenr.write(|w| w.iopeen().set_bit());

    let gpioe = &peripherals.GPIOE;

    // Set Pin 9 to output
    gpioe.moder.write(|w| w.moder9().output());

    loop {
        // Read Port E Pin 9 output data register
        if gpioe.odr.read().odr9().bit_is_set() {
            // Write to bit reset register to set Pin 9 Low
            gpioe.bsrr.write(|w| w.br9().set_bit());
        } else {
            // Write to bit set register to set Pin 9 High
            gpioe.bsrr.write(|w| w.bs9().set_bit());
        }

        unsafe {
            delay();
        }
    }
}

unsafe fn delay() {
    // Delay for approx 1s - found 80.000 by "brute force"
    for _ in 0..80_000 {
        asm!("nop");
    }
}
```

First thing you probably noticed is the much reduced code we need to write when using the hal crate. Still we need to use the `no_std`, `no_main` and `entry` tag, but we can omit the pointers to the registers and we do not need to use the `unsafe` keyword anymore.

On to the actual implementation.

```rust
#[entry]
fn main() -> ! {
    // You should see that in your openocd output
    hprintln!("Hello from Discovery");

    let peripherals = stm32f303::Peripherals::take().unwrap();

    let rcc = &peripherals.RCC;
    // Set HSI clock on
    rcc.cr.write(|w| w.hsion().set_bit());
    // Enable GPIO Port E clock
    rcc.ahbenr.write(|w| w.iopeen().set_bit());

    let gpioe = &peripherals.GPIOE;

    // Set Pin 9 to output
    gpioe.moder.write(|w| w.moder9().output());
}
```

We still need to initialise the microcontroller and Port E. But now we can do that by leveraging the hal crate. We _take_ the Peripherals which gives us access to the needed functions. _Taking_ the Peripherals can only be done once in the whole program. In our case, this is not a problem, but it gets trickier as soon as you need to access a peripheral from another function or an interrupt handler. We'll explore that in Part 3.

The nice thing about the hal is, that we do need to care about register addresses or offsets - it's all baked in and generated from the svd file into the hal crate. We get typed and named properties which we can use to control everything - down to the bits.

```rust
loop {
    // Read Port E Pin 9 output data register
    if gpioe.odr.read().odr9().bit_is_set() {
        // Write to bit reset register to set Pin 9 Low
        gpioe.bsrr.write(|w| w.br9().set_bit());
    } else {
        // Write to bit set register to set Pin 9 High
        gpioe.bsrr.write(|w| w.bs9().set_bit());
    }

    unsafe {
        delay();
    }
}
```

The loop again is straight forward and should be self-explanatory.

Last we insert a delay of about 1s before we start the loop again.

On to Part 3 -> [Rust Embedded: Blinky - Part 3]({% post_url 2025-01-16-rust-embedded-blinky-part-3 %})
