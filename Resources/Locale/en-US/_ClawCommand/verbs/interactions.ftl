# Verb category
verb-categories-interaction = Interactions

# Verb status messages
interaction-verb-invalid = Some requirements for this verb are not met. You cannot use it right now.
interaction-verb-cooldown = This verb is on cooldown. Wait {TOSTRING($seconds, "F1")} seconds.
interaction-verb-too-strong = You are too strong to use this verb.
interaction-verb-too-weak = You are too weak to use this verb.
interaction-verb-invalid-target = You cannot use this verb on that target.
interaction-verb-no-hands = You have no usable hands.
interaction-verb-cannot-reach = You cannot reach there.
interaction-verb-unconscious = You cannot use this verb while unconscious.

# Noop interactions

interaction-LookAt-name = Look at
interaction-LookAt-description = Stare into the void and see it stare back.
interaction-LookAt-success-self-popup = You look at {THE($target)}.
interaction-LookAt-success-target-popup = You feel {THE($user)} looking at you...
interaction-LookAt-success-others-popup = {THE($user)} looks at {THE($target)}.

interaction-CheckOut-name = Check out
interaction-CheckOut-description = This lets you check someone out on the down low, only you and they will know you did.
interaction-CheckOut-success-self-popup = You are really eyeballing {THE($target)}.
interaction-CheckOut-success-target-popup = You think that {THE($user)} might be checking you out...

interaction-Hug-name = Hug
interaction-Hug-description = A hug a day keeps the psychological horrors beyond your comprehension away.
interaction-Hug-success-self-popup = You hug {THE($target)}.
interaction-Hug-success-target-popup = {THE($user)} hugs you.
interaction-Hug-success-others-popup = {THE($user)} hugs {THE($target)}.

interaction-Pet-name = Pet
interaction-Pet-description = Pet your co-worker to ease their stress.
interaction-Pet-success-self-popup = You pet {THE($target)} on {POSS-ADJ($target)} head.
interaction-Pet-success-target-popup = {THE($user)} pets you on your head.
interaction-Pet-success-others-popup = {THE($user)} pets {THE($target)}.

interaction-PetAnimal-name = {interaction-Pet-name}
interaction-PetAnimal-description = Pet an animal.
interaction-PetAnimal-success-self-popup = {interaction-Pet-success-self-popup}
interaction-PetAnimal-success-target-popup = {interaction-Pet-success-target-popup}
interaction-PetAnimal-success-others-popup = {interaction-Pet-success-others-popup}

interaction-KnockOn-name = Knock
interaction-KnockOn-description = Knock on the target to attract attention.
interaction-KnockOn-success-self-popup = You knock on {THE($target)}.
interaction-KnockOn-success-target-popup = {THE($user)} knocks on you.
interaction-KnockOn-success-others-popup = {THE($user)} knocks on {THE($target)}.

interaction-Rattle-name = Rattle
interaction-Rattle-success-self-popup = You rattle {THE($target)}.
interaction-Rattle-success-target-popup = {THE($user)} rattles you.
interaction-Rattle-success-others-popup = {THE($user)} rattles {THE($target)}.

interaction-WaveAt-name = Wave at
interaction-WaveAt-description = Wave at the target. If you are holding an item, you will wave it.
interaction-WaveAt-success-self-popup = You wave {$hasUsed ->
    [false] at {THE($target)}.
    *[true] your {$used} at {THE($target)}.
}
interaction-WaveAt-success-target-popup = {THE($user)} waves {$hasUsed ->
    [false] at you.
    *[true] {POSS-ADJ($user)} {$used} at you.
}
interaction-WaveAt-success-others-popup = {THE($user)} waves {$hasUsed ->
    [false] at {THE($target)}.
    *[true] {POSS-ADJ($user)} {$used} at {THE($target)}.
}

# Help interactions

interaction-HelpUp-name = Help up
interaction-HelpUp-description = Help the person get up.
interaction-HelpUp-delayed-self-popup = You try to help {THE($target)} get up...
interaction-HelpUp-delayed-target-popup = {THE($user)} tries to help you get up...
interaction-HelpUp-delayed-others-popup = {THE($user)} tries to help {THE($target)} get up...
interaction-HelpUp-success-self-popup = You help {THE($target)} get up.
interaction-HelpUp-success-target-popup = {THE($user)} helps you up.
interaction-HelpUp-success-others-popup = {THE($user)} helps {THE($target)} up.
interaction-HelpUp-fail-self-popup = You fail to help {THE($target)} get up.
interaction-HelpUp-fail-target-popup = {THE($user)} fails to help you up.

interaction-ForceDown-name = Force down
interaction-ForceDown-description = Force the person to lay down on the floor.
interaction-ForceDown-delayed-self-popup = You try to force {THE($target)} down...
interaction-ForceDown-delayed-target-popup = {THE($user)} tries to force you down...
interaction-ForceDown-delayed-others-popup = {THE($user)} tries to force {THE($target)} down...
interaction-ForceDown-success-self-popup = You force {THE($target)} to lay down.
interaction-ForceDown-success-target-popup = {THE($user)} forces you to lay down.
interaction-ForceDown-success-others-popup = {THE($user)} forces {THE($target)} to lay down.
interaction-ForceDown-fail-self-popup = You fail to force {THE($target)} down.
interaction-ForceDown-fail-target-popup = {THE($user)} fails to force you down.

interaction-MakeSleepOther-name = Make sleep
interaction-MakeSleepOther-description = Put the target to sleep.
interaction-MakeSleepOther-delayed-self-popup = You are trying to put {THE($target)} to sleep...
interaction-MakeSleepOther-delayed-target-popup = {THE($user)} is trying to put you to sleep...
interaction-MakeSleepOther-delayed-others-popup = {THE($user)} is trying to put {THE($target)} to sleep...
interaction-MakeSleepOther-fail-self-popup = You fail to put {THE($target)} to sleep.
interaction-MakeSleepOther-fail-target-popup = {THE($user)} fails to put you to sleep.
interaction-MakeSleepOther-success-self-popup = You put {THE($target)} to sleep.
interaction-MakeSleepOther-success-target-popup = {THE($user)} puts you to sleep.
interaction-MakeSleepOther-success-others-popup = {THE($user)} puts {THE($target)} to sleep.

interaction-ShakeOther-name = Shake
interaction-ShakeOther-description = Shake the target.
interaction-ShakeOther-fail-self-popup = You somehow fail to shake {THE($target)}.
interaction-ShakeOther-fail-target-popup = {THE($user)} somehow fails to shake you.
interaction-ShakeOther-success-self-popup = You grab and shake {THE($target)}.
interaction-ShakeOther-success-target-popup = {THE($user)} grabs and shakes you.
interaction-ShakeOther-success-others-popup = {THE($user)} grabs and shakes {THE($target)}.

# Self interactions

interaction-PinchSelf-name = Pinch yourself
interaction-PinchSelf-description = They say it helps you make sure the hell that goes around you is not a dream.
interaction-PinchSelf-success-self-popup = You pinch yourself... Ouch!
interaction-PinchSelf-success-others-popup = {THE($user)} pinches {REFLEXIVE($user)}... Looks painful!
interaction-PinchSelf-fail-self-popup = You somehow fail to pinch yourself. Better for you.
interaction-PinchSelf-delayed-self-popup = You pinch yourself...
interaction-PinchSelf-message-1 = Ouchh!!
interaction-PinchSelf-message-2 = Aaaah!!
interaction-PinchSelf-message-3 = Ow!!

interaction-MakeSleepSelf-name = Sleep
interaction-MakeSleepSelf-description = Put yourself to sleep.
interaction-MakeSleepSelf-delayed-self-popup = You are trying to fall asleep...
interaction-MakeSleepSelf-fail-self-popup = You cannot sleep right now.
interaction-MakeSleepSelf-success-self-popup = You put yourself to sleep.
interaction-MakeSleepSelf-success-others-popup = {THE($user)} falls asleep.

interaction-Kiss-name = Kiss
interaction-Kiss-description = Plant a smooch on someone special.
interaction-Kiss-success-self-popup = You kiss {THE($target)}.
interaction-Kiss-success-target-popup = {THE($user)} kisses you!
interaction-Kiss-success-others-popup = {THE($user)} kisses {THE($target)}.

interaction-Lick-name = Lick
interaction-Lick-description = Give something a good lick. Why? Only you know.
interaction-Lick-success-self-popup = You lick {THE($target)}.
interaction-Lick-success-target-popup = {THE($user)} licks you!
interaction-Lick-success-others-popup = {THE($user)} licks {THE($target)}.

interaction-Bite-name = Bite
interaction-Bite-description = Sink your teeth into someone. This will hurt.
interaction-Bite-success-self-popup = You bite {THE($target)}!
interaction-Bite-success-target-popup = {THE($user)} bites you!
interaction-Bite-success-others-popup = {THE($user)} bites {THE($target)}!
interaction-Bite-fail-self-popup = You fail to bite {THE($target)}.
interaction-Bite-fail-target-popup = {THE($user)} tries to bite you but fails.
