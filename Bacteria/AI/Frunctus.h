//
//  Frunctus.h
//  Bacterium
//
//  Created by Giorgi Abelashvili on 3/15/13.
//
//

#import "EnemyAgent.h"
//
@interface Frunctus : EnemyAgent
{
    float spiralAlpha;
    int radius;
    int alphaDelta;
    int perpendicularRandomer;
    BOOL timeToEscape;
    CGPoint escapePoint;
    float prevShootRate;
}
//
@property (nonatomic,assign) BOOL timeToEscape;
//
-(void) moveOnCircle;
@end
