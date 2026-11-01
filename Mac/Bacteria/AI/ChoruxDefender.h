//
//  ChoruxDefender.h
//  Bacterium
//
//  Created by Giorgi Abelashvili on 3/5/13.
//
//

#import "SpiralCorosia.h"


@interface ChoruxDefender : SpiralCorosia
{
    CGPoint pointToDeffend;
    EnemyAgent *targetToDeffend;
    BOOL canHit;
}
@property (nonatomic,assign) EnemyAgent *targetToDeffend;
@property (nonatomic,assign) CGPoint pointToDeffend;
@end
