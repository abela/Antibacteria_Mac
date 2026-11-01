//
//  PeceWalkerGameMode.h
//  AntiBacteria
//
//  Created by Giorgi Abelashvili on 5/9/13.
//
//

#import "GameMode.h"

@interface PeceWalkerGameMode : GameMode
{
    int lifeBonsGeneratorCounter;
    float realTimeGameTickCounter;
    int totalGameModeTime;
    BOOL firstBonusHasCreated;
}
-(void) realTimeGameTick;
@end
