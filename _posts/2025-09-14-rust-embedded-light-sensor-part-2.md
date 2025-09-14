---
layout: post
tags: rust embedded stm32f3
slug: rust-embedded-light-sensor
title: "Rust Embedded: Light Sensor - Part 2"
description: Reading values continuously from a light sensor with the stm32f3xx-hal crate.
---

In [Part 1]({% post_url 2025-07-18-rust-embedded-light-sensor-part-1 %}) we used the _stm32f303_ crate and coded out the whole setup/calibration of the ADC on our own. In this short post, we will use the _stm32f3xx-hal_ crate, which makes this a lot easier.

Code on [GitHub](https://github.com/eisnstein/rust-embedded-light-sensor-hal/blob/main){:target="\_blank" rel="noopener noreferrer"}.

```rust
#![no_main]
#![no_std]

extern crate panic_halt;

use cortex_m::asm;
use cortex_m_rt::entry;
use rtt_target::rtt_init_defmt;
use stm32f3xx_hal::{
    adc::{Adc, CommonAdc, config::Config},
    pac,
    prelude::*,
};

#[entry]
fn main() -> ! {
    rtt_init_defmt!();
    defmt::info!("Starting light sensor application");

    let dp = pac::Peripherals::take().unwrap();

    let mut flash = dp.FLASH.constrain();
    let mut rcc = dp.RCC.constrain();

    // Freezes the clock configuration, making it effective.
    let clocks = rcc.cfgr.freeze(&mut flash.acr);
    // Splits the GPIO block into independent pins and registers.
    let mut gpioa = dp.GPIOA.split(&mut rcc.ahb);
    // Configures the pin to operate as an analog pin, with disabled schmitt trigger.
    let mut light_pin = gpioa.pa0.into_analog(&mut gpioa.moder, &mut gpioa.pupdr);

    // Initialize a new ADC peripheral.
    let mut adc = Adc::new(
        dp.ADC1,
        Config::default(),
        &clocks,
        &CommonAdc::new(dp.ADC1_2, &clocks, &mut rcc.ahb),
    );

    loop {
        let data: u16 = adc.read(&mut light_pin).unwrap();
        defmt::info!("{}", data);

        // Wait 100ms for next conversion
        asm::delay(800_000);
    }
}
```

Using the _hal_ crate takes a lot of the setup "under the hood" and we can concentrate on the actual implementation.