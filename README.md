# c-sharp-thermastat
Thermastat using C# for code that cools based on humidity and heats based on temp

#Testing relay from Terminal
gpioset --chip gpiochip0 17=1 27=1 22=1 23=1
-> all off
gpioset --chip gpiochip0 17=0
-> gpi 17 on

#Run Test suite
dotnet test

#Source of outside weather
https://api.weather.gov/stations/KMKG/observations/latest
