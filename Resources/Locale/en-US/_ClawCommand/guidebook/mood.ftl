entity-effect-guidebook-add-moodlet =
    { $chance ->
        [1] { $deltasign ->
                [-1] Worsens
               *[other] Improves
             }
       *[other] { $deltasign ->
                    [-1] worsen
                   *[other] improve
                 }
    } mood by [color=white]{ $amount }[/color]{ $useEffectName ->
        [true] { " " }([bold]{ $moodEffect }[/bold])
       *[other] { "" }
    }{ $timeout ->
        [0] { "" }
       *[other] { " " }for [color=white]{ $timeout }[/color] seconds
    }

entity-condition-guidebook-has-moodlet =
    { $inverted ->
        [true] it does not have
       *[other] it has
    } [bold]{ $effect }[/bold]
