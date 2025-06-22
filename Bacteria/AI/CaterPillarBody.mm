//
//  CaterPillarBody.m
//  Bacterium
//
//  Created by Giorgi Abelashvili on 3/4/13.
//
//

#import "CaterPillarBody.h"
#import "GameResourceManager.h"
#import "AiInput.h"
//
//
@implementation CaterPillarBody
-(id) createNextCaterpillarBodyAtPosition:(CGPoint)position
{
    if ((self = [super init])) {
        type = kEnemy;
        sprite = [CCSprite spriteWithSpriteFrameName:@"Enemy3.png"];
        sprite.position = position;
        flag = kDefault;
		//
		width = 0.95f;
		height = 0.95f;
        hitCount = 10;
        //
        [[[GameResourceManager sharedGameResourceManager] sharedMainCharacterSpriteSheet] addChild:sprite z:1];
        //
        position.x /= ASPECT_RATIO;
        position.y /= ASPECT_RATIO;
        [super createNewBodyAt:position andDimension:ccp(width,height)];
        //
        body->GetFixtureList()->SetSensor(true);
        //
        return self;
    }
    return nil;
}
//
-(void) hit
{
    if(tag == 0)
    {
        hitCount -- ;
        flag = (hitCount <= 0) ? kDealloc : kDefault;
    }
}
//
-(void) bigExploded
{
    if(tag == 0)
    {
        if(ccpDistance([[AiInput sharedAiInput] mainCharacterPosition], sprite.position) <= BIG_EXPLOSION_DISTANCE)
        {
            hitCount -=BIG_EXPLOSION_HIT_CONST ;
            flag = (hitCount <= 0) ? kDealloc : kDefault;
        }
    }
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
    [sprite removeFromParentAndCleanup:YES];
    [[Figure sharedPhysicsWorld] getPhysicsWorld]->DestroyBody(body);
}
//
@end
