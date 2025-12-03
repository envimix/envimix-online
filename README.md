# Envimix Online

### Warning. This is not a representative code. The goal was just to finish the project in some way so that players can enjoy Envimix Turbo finally. Refer to this just if you want to see how something works.

Envimix Online is the overall backend behind the Envimix project. It includes the main Envimix Turbo Web API, the Envimix Discord bot, and the main website under https://envimix.gbx.tools.

## Envimix Web API

The main logical online service behind the Envimix project, currently only Envimix Turbo.

### Envimania

#### Because 1 map =/= 1 record.

Envimania stores records in a different way compared to Dedimania or server controllers. Records are sent with an additionally provided score context. This score context is effectively a combination of **record types**. Users can in theory combine gameplay styles as they wish. Sent record has to contain every record type and a valid combination of gameplay styles. Certain gameplay styles can be incompatible (configurable on server side).

### Track of the day

Track of the Day feature (that you might know from TM2020) came out first in the Nadeo Envimix title pack back in 2018, and it makes a comeback in Envimix 2025! From where do you got inspired Nadeo? 🤔

### Difficulty and Quality rating

Known way from Nadeo Envimix and Challenge to rate a **gameplay style combination**, because sometimes it isn't as simple as good or bad.

### Activity points and WR/PB

Two alternative ways (invented by Poutrel) to value records that don't have as many competitors.

### Stars

Each **gameplay style combination** can be rated with a special star to highlight something special about it, so that players with no clue can hook onto something. Only clients with special permissions can give a star.

## Envimix Website

The site that is meant to run in web browsers. Very much WIP.

## Envimix Discord Bot

Currently only serving Envimix TM2020 validation system.
