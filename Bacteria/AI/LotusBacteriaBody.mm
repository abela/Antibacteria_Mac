//
//  LotusBacteriaBody.m
//  Bacterium
//
//  Created by Giorgi Abelashvili on 3/6/13.
//
//

#import "LotusBacteriaBody.h"

@implementation LotusBacteriaBody
//
-(id) createAtPosition:(CGPoint)position
{
    if((self = [super createAtPosition:position])) {
        
        type = kEnemy;
        enemyAgentType = kLotusBacteriusBody;
        hitCount = 1;
        speed = 7.0f;
        vitaminsCount = 3;
        return self;
    }
    return nil;
}
//
-(void) creatingAnimationHasEnd:(id)sender
{
    [super creatingAnimationHasEnd:sender];
    agentStateType = kStateWaiting;
}
//
-(void) update
{
	[super update];
}
//
-(void) patrol
{
	[super patrol];
}
//
-(void) releaseAgent
{
    //
    if(hitCount <= 0)
    {
        for (int i =0; i<vitaminsCount; i++) {
            CGPoint randomPos = [[VitaminManager sharedVitaminManager] getValidBonusSpawnPointAtPoint:sprite.position];
            [[VitaminManager sharedVitaminManager] createVitaminAtPosition:randomPos withScore:1];
        }
    }
    //
	[super releaseAgent];
}
//
-(void) hit
{
    if(tag == 0)
    {
        [super hit];
    }
}
//
-(void) bigExploded
{
    if(tag == 0)
    {
        [super bigExploded];
    }
}
//
@end
