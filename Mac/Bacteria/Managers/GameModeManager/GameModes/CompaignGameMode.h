//
//  CompaignGameMode.h
//  AntiBacteria
//
//  Created by Giorgi Abelashvili on 5/9/13.
//
//

#import "GameMode.h"

@interface CompaignGameMode : GameMode
{
    NSTimer *realTimeTickVar;
    BOOL weHaveBossLevel;
    BOOL isFirstRun;
    int realTimeTickCounter;
    float originalEnemyCreateVar;
}
-(void) loadNextCompaignLevel;
-(void) realTimeTick:(NSTimer*)timer;
-(void) levelHasWon;
@end
