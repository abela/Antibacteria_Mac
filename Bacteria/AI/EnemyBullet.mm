//
//  EnemyBullet.m
//  Bacterium
//
//  Created by Giorgi Abelashvili on 3/3/13.
//
//

#import "EnemyBullet.h"

@implementation EnemyBullet
-(id) initAtPosition:(CGPoint)position
         andRotation:(float)angle
        andDirection:(CGPoint)direction
       andStartDelta:(float)startDelta
{
    //
    if((self = [super initAtPosition:position
                         andRotation:angle
                        andDirection:direction
                        andStartDelta:startDelta]))
    {
        return self;
    }
    //
    return nil;
    //
}
@end
