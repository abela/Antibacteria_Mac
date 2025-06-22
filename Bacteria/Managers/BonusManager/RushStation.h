//
//  RushStation.h
//  AntiBacteria
//
//  Created by Giorgi Abelashvili on 5/13/13.
//
//

#import "BonusObject.h"

@interface RushStation : BonusObject
{
    BOOL characterIsIn;
    CCProgressTimer *lifeProgressTimer;
    float opacityCounterStep;
}
@property (nonatomic,assign) BOOL characterIsIn;
@property (nonatomic,retain) CCProgressTimer *lifeProgressTimer;
@end
