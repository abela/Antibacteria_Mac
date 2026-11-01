//
//  EnemyDirection.h
//  AntiBacteria
//
//  Created by Giorgi Abelashvili on 8/14/13.
//
//

#import "Figure.h"

@interface EnemyDirection : Figure
{
    CGPoint enemyAgentPosition;
}
//
@property (nonatomic,assign) CGPoint enemyAgentPosition;
//
-(id) addTargetAtPosition:(CGPoint)position;
-(void) removeTarget;
-(void) updateTarget;
@end
