//
//  SpiralCorosia.h
//  Bacterium
//
//  Created by Giorgi Abelashvili on 3/2/13.
//
//

#import "EnemyAgent.h"

@interface SpiralCorosia : EnemyAgent
{
    float spiralAlpha;
    int radius;
    int alphaDelta;
}
-(void) moveOnCircle;
@end
