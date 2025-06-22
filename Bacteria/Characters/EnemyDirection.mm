//
//  EnemyDirection.m
//  AntiBacteria
//
//  Created by Giorgi Abelashvili on 8/14/13.
//
//

#import "EnemyDirection.h"
#import "GameResourceManager.h"

@implementation EnemyDirection
//
@synthesize enemyAgentPosition;
//
-(id) addTargetAtPosition:(CGPoint)position
{
    //
    sprite = [CCSprite spriteWithSpriteFrameName:@"enemydirection.png"];
    sprite.position = position;
    sprite.scale = 0.5f;
    [[[GameResourceManager sharedGameResourceManager] sharedEnemyArrowDirectioSpriteSheet] addChild:sprite z:1 tag:1];
    //
    return self;
}
//
-(void) updateTarget
{
    if (flag == kDefault) {
        // update target position around the circle
    }
}
//
-(void) removeTarget
{
    [sprite removeFromParentAndCleanup:YES];
}
//
@end
