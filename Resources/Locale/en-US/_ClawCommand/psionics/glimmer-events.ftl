# Claw Command - station announcements for glimmer events.
#
# Glimmer events shipped silent upstream, so a station could get mites in the vents or every psion on
# the crew stunned with nothing to tell anyone why. Since CCGlimmerScheduler fires these off the
# glimmer level, the crew now gets told the noosphere did something.
#
# Deliberately phenomenon-framed rather than naming the threat: these read as Epistemics reporting a
# sensor reading, not as CentComm handing out a monster manifest. The five spawn events share one
# message (see BaseGlimmerSignaturesEvent) so the announcement never says which creature turned up.

station-event-glimmer-signatures-announcement = New psionic signatures are manifesting aboard the station. Epistemics is advised to locate and catalogue them.

station-event-mundane-discharge-announcement = A minor noöspheric discharge has been absorbed by the station's psionic infrastructure. No action is required.

station-event-noospheric-zap-announcement = Noöspheric discharge detected. Psionically active crew may experience disorientation and impaired speech.

station-event-noospheric-fry-announcement = Severe noöspheric overload. Psionic insulation is burning out across the station, and glimmer-reactive equipment is discharging. Keep clear of probers.

station-event-psionic-cat-got-your-tongue-announcement = Noöspheric interference is suppressing vocal centres in psionically active crew. Expect communication difficulties.
