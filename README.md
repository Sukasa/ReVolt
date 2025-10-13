# Re-Volt!

A stationeers mod which overhauls the game's electrical simulation.

## Features 
- Cables no longer burn instantly if you draw too much power, instead burning after a delay based on how much you're overloading them
- Fuses still protect cables, burning out immediately
- If you have multiple power sources on a grid, such as multiple Batteries, load is distributed equally among them


## Future Content
- Circuit breakers, which are time-delay fuses, but also resettable instead of being single-use.  Coming in Small, Large, and Smart variants
- Limitations on station battery charge/discharge rates.  You may need to parallel batteries for high loads now
- Limitations on APC power capacity, before the APC burns out
- Brownouts
- Arc Faults, both in breakers and if cables are damaged externally
- Modular Batteries, splitting Batteries / Large Batteries into Chargers, Battery Banks, and Inverters.
- AC and DC power concepts.  Don't connect your generators directly to your Battery Banks, use a Charger!

# Development

This is still very much in development, and reach out on the Stationeers modding discord if you need any assistance!

# Credits

This mod stands on the shoulders of some serious giants.  None of this could have happened without the Stationeers modding community, but I'd like to give some extra-special thanks to the many folks who helped me, including:

- tom_is_unlucky, for sharing the FPGA mod as a reference, and for so much of the framework code
- Inaki, for general help and for their modding tools
- Chipstix213, for general advice and for the Ball example mod
- WIKUS, for doing such a damn good job with Modular Consoles that it inspired me to give this a shot
- Spacebuilder2020, for help and encouragement
- Thyra, whose troubles with their Elevator project indirectly gave me a lot of answers right before I needed them