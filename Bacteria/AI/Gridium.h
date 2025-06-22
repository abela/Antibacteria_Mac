//
//  Gridium.h
//  Bacterium
//
//  Created by Giorgi Abelashvili on 3/2/13.
//
//
//
#import "EnemyAgent.h"

@interface Gridium : EnemyAgent
{
    CGPoint startPos;
    CGPoint endPos;
    float rotAlpha;
}
//
@property (nonatomic,assign) CGPoint startPos;
@property (nonatomic,assign) CGPoint endPos;
@property (nonatomic,assign) float rotAlpha;
//
@end
