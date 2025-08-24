---
layout: post
tags: rust embedded stm32f3
slug: rust-embedded-light-sensor
title: "Rust Embedded: Light Sensor - Part 1"
description: Reading values continuously from a light sensor.
---

I wanted build a little project where i could use the ADC of the STM32F303VC microcontroller. I came up with the idea to read values from a light sensor. So the light sensor is connected to the microcontroller via pin 0 of port A. We need to convert the voltage on this pin to digital values. That is where the ADC comes in - we convert from _analog_ to _digital_.

Code on [GitHub](https://github.com/eisnstein/rust-embedded-light-sensor/blob/main){:target="\_blank" rel="noopener noreferrer"}.

Here is the full code example for our program with the light sensor. Further down we'll go through the code in more detail. 

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
    let cp = cortex_m::Peripherals::take().unwrap();

    let mut delay = cortex_m::delay::Delay::new(cp.SYST, 8_000_000);
    let rcc = &peripherals.RCC;
    let gpioa = &peripherals.GPIOA;
    let adc1 = &peripherals.ADC1;
    let common_adc12 = &peripherals.ADC1_2;

    // Configure Port A Pin 0

    // Set HSI clock on
    rcc.cr.write(|w| w.hsion().on());
    // Set Pin 0 to analog input
    gpioa.moder.write(|w| w.moder0().analog());
    // Set Pin 0 to floating (disable schmitt trigger)
    gpioa.pupdr.write(|w| w.pupdr0().floating());

    // Enable GPIO Port A clock
    rcc.ahbenr.write(|w| w.iopaen().enabled());
    // Enable ADC1 clock
    rcc.ahbenr.write(|w| w.adc12en().enabled());
    // Set ADC clock mode
    common_adc12.ccr.write(|w| w.ckmode().sync_div4());

    // Configure ADC1

    // Set single conversion mode
    adc1.cfgr.write(|w| w.cont().single());
    // 12bit data resolution
    adc1.cfgr.write(|w| w.res().bits10());
    // Select channel 1
    adc1.sqr1.write(|w| unsafe { w.sq1().bits(1) });
    // Set sample time
    adc1.smpr1.write(|w| w.smp1().cycles601_5());

    // Reset control register
    adc1.cr.reset();

    hprintln!("Enable vrs");
    // Enable voltage regulation sequence.
    // This has to be done before the calibration.
    adc1.cr.write(|w| w.advregen().intermediate());
    adc1.cr.write(|w| w.advregen().enabled());

    // Wait for the startup time of the ADC voltage regulator
    // see STM32f303 reference manual section 15.3.6
    delay.delay_us(80);

    hprintln!("Start calibration");
    // Start calibration
    adc1.cr.write(|w| w.adcaldif().single_ended());
    adc1.cr.write(|w| w.adcal().calibration());
    while adc1.cr.read().adcal().is_calibration() {
        hprintln!("hoho");
    }

    hprintln!("Disable vrs");
    // Disable voltage regulation sequence.
    adc1.cr.write(|w| w.advregen().intermediate());
    adc1.cr.write(|w| w.advregen().disabled());

    hprintln!("Enable adc");
    // Enable the ADC
    adc1.cr.write(|w| w.aden().enabled());
    while adc1.isr.read().adrdy().is_not_ready() {}

    hprintln!("Start loop");
    loop {
        // Start conversion and wait until ECO and EOS is set.
        adc1.cr.write(|w| w.adstart().start_conversion());
        while adc1.isr.read().eoc().is_not_complete() && adc1.isr.read().eos().is_not_complete() {}

        // Read data from data register
        let data = adc1.dr.read().rdata().bits();

        hprintln!("{}", data);

        // Wait 100ms for next conversion
        delay.delay_ms(100);
    }
}
```

To be able to use a button to toggle the LED state we need to use one more GPIO port. We could go the naive way and 
implement a loop which checks, if the button is clicked and - depending on the state - update 
the output for the LED. But we are not going this way and do it the right way - by leveraging interrupts. Basically we 
register a pin (which will be connected to the button) as trigger for an interrupt. That will execute the interrupt handler 
where the actual toggeling of the LED will happen.

On to the actual implementation.

```rust
static EXT_I: Mutex<RefCell<Option<EXTI>>> = Mutex::new(RefCell::new(None));
static GPIO_E: Mutex<RefCell<Option<GPIOE>>> = Mutex::new(RefCell::new(None));
```

Here is the thing. We are _taking_ the `Peripherals` in our main function. As I already stated in [Part 2]({% post_url 2025-01-06-rust-embedded-blinky-part-2 %}), we only can _take_ the `Peripherals` once in the whole program.
However, if we want to toggle the LED in another function - our interrupt handler - we need a way to access the GPIO port from there. The solution is, that we need to create a static "global" variable - in our case 2: `EXT_I` and `GPIO_E`, which we can then access everywhere. More on that later.

```rust
let peripherals = stm32f303::Peripherals::take().unwrap();

let syscfg = &peripherals.SYSCFG;
let exti = &peripherals.EXTI;

// Initialize EXT interrupt

// Set Pin 0 from Port A as input for EXTI0
syscfg.exticr1.write(|w| w.exti0().pa0());
// Disable mask on EXTI0
exti.imr1.write(|w| w.mr0().set_bit());
// Set rising trigger disabled
exti.rtsr1.write(|w| w.tr0().disabled());
// Set falling trigger enabled
exti.ftsr1.write(|w| w.tr0().enabled());
// Enable interrupt
unsafe {
    NVIC::unmask(stm32f303::Interrupt::EXTI0);
}
```

My idea was, that I want to press a button, which is connected to a pin on the microcontroller, and toggle the LED. So when I press the button, an event should be emitted, which then triggers the call of a function where the toggle logic is executed. For that, _interrupts_ exist. Via the _extended interrupts and events controller_ we can configure external and internal interrupts. In our case it is an external interrupt.

You can find a description on how to configure an interrupt under **14. Interrupts and events** (14.2.5 to be precise) from the [STM32 Reference Manual]({% post_url 2024-06-03-rust-embedded-setup-part-0 %}).

We chose Pin 0 from Port A as our input where the button is going to be connected. So we configure EXTI0 to be triggered by Pin 0 from Port A by `w.exti0().pa0()` on the _SYSCFG external interrupt configuration register 1_ (exticr1). Under 14.2.6 you can find which Pins from which Ports map to which EXT interrupt event lines. Next we need to set bit 0 in the _interrupt mask register 1_ to 1 - basically enabling the external interrupt line 0 (where our button is connected to).
We want our event to trigger on falling edge - so we disable _rising trigger_ and enable _falling trigger_.
Last but not least we need to enable the EXTI0 interrupt in the _Nested vectored interrupt controller_ (NVIC).

```rust
let rcc = &peripherals.RCC;
// Set HSI clock on
rcc.cr.write(|w| w.hsion().set_bit());
// Enable GPIO Port E and A clock
rcc.ahbenr.write(|w| w.iopaen().enabled());
rcc.ahbenr.write(|w| w.iopeen().enabled());
// Enable SYSCFG clock
rcc.apb2enr.write(|w| w.syscfgen().enabled());

let gpioa = &peripherals.GPIOA;
// Set Pin 0 to input
gpioa.moder.write(|w| w.moder0().input());

let gpioe = &peripherals.GPIOE;
// Set Pin 9 to output
gpioe.moder.write(|w| w.moder9().output());
```

Next we again need to configure the system clock and enable GPIO Port E and A. We use Port E (output) to connect to the LED and Port A (input) to connect to the button. 
Important is to also enable the SYSCFG clock in the _APB2 peripheral clock enable register_. The EXTI controller relies on the SYSCFG module to route 
GPIO pins to specific EXTI lines. Without enabling the SYSCFG clock the external interrupt configuration will not work correctly.

```rust
// Put EXTI and GPIOE into the global variable to be able to
// access Port E in the EXTI0 interrupt handler.
cortex_m::interrupt::free(|cs| {
    EXT_I.borrow(cs).replace(Some(peripherals.EXTI));
    GPIO_E.borrow(cs).replace(Some(peripherals.GPIOE));
});

loop {}
```

At the beginning of our program, we defined to static global variables. To make sure, that only always one actor can access those values, we put them in a mutex. 
We have to use an Option there, because initially we didn't have the values. Now, at the end of our programm we have those values - `EXTI` and `GPIOE` peripherals - 
which we place into our static vars. We do that within an callback to the `cortex_::interrupt::free` function. This disables all interrupts temporarily. 
Last we call `loop {}` to run an endless loop and wait for events from our button.

```rust
#[interrupt]
fn EXTI0() {
    cortex_m::interrupt::free(|cs| {
        if let Some(exti) = EXT_I.borrow(cs).borrow().deref() {
            // Clear pending register
            exti.pr1.write(|w| w.pr0().set_bit());
        }

        if let Some(gpioe) = GPIO_E.borrow(cs).borrow().deref() {
            // Read Port E Pin 9 output data register
            if gpioe.odr.read().odr9().bit_is_set() {
                // Write to bit reset register to set Pin 9 Low
                gpioe.bsrr.write(|w| w.br9().set_bit());
            } else {
                // Write to bit set register to set Pin 9 High
                gpioe.bsrr.write(|w| w.bs9().set_bit());
            }
        }
    })
}
```

The `EXTI0` interrupt handler is the function that gets called when the EXTI0 interrupt is triggered. Important here is, that the function actually is called `EXTI0` and the `#[interrupt]` attribute.
Like before we call the `cortex_m::interrupt::free` function to execute the enclosed closure in a critical section, ensuring that no other interrupts can occur during its execution.
First we need to clear bit 0 of the _pending register_ (`pr1`). This acknowledges the interrupt and allows it to be triggered again in the future.
Second, we toggle the state of Port E Pin 9. The output data register (`odr`) for Port E Pin 9 is read to check if the pin is currently set (high). If the pin is set, the bit reset register (`bsrr`) is written to set Pin 9 low. If the pin is not set, the bit set register (`bsrr`) is written to set Pin 9 high.

