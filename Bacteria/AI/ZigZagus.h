//
//  ZigZagus.h
//  Bacterium
//
//  Created by Giorgi Abelashvili on 3/8/13.
//
//

#import "EnemyAgent.h"

@interface ZigZagus : EnemyAgent
{
    CGPoint nextZigZagPoint;
    float sinAlpha;
    float sineMoveSpeed;
    float alphaConst;
}
@end
