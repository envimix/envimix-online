# Envimix Online

## Envimix Web API

The main logical online service behind the Envimix project.

This application can be also applied outside of Envimix, though. Currently, it expects ManiaPlanet authentication, but it is possible to use different authentication methods.

## Envimania

### Because 1 map =/= 1 record.

Envimania stores records in a different way compared to Dedimania or server controllers. Records are sent with an additionally provided score context. This score context is effectively a table of **record types** as a column and **gameplay style combinations** as a row, which can be modified in the configuration at the server's wish. Users can combine gameplay styles as they wish. Sent record has to contain every record type and a valid combination of gameplay styles. Certain gameplay styles can be incompatible (configurable on server side).

## Track of the day

Track of the Day feature (that you might know from TM2020) came out first in the Nadeo Envimix title pack back in 2018, and it makes a comeback in Envimix 2023! From where do you get inspired Nadeo? 🤔

## Difficulty and Quality rating

Known way from Nadeo Envimix and Challenge to rate a **gameplay style combination**, because sometimes it isn't as simple as good or bad.

## Activity points and WR/PB

Two alternative ways (invented by Poutrel) to value records that don't have as many competitors.

## Stars

Each **gameplay style combination** can be rated with a special star to highlight something special about it, so that players with no clue can hook onto something. Only clients with special permissions can give a star.
