//
//  BonusObject.m
//  Bacterium
//
//  Created by Giorgi Abelashvili on 3/30/13.
//
//

#import "BonusObject.h"
#import "Figure.h"

@implementation BonusObject
@synthesize bonusObjectType;
//
-(id) initWithType:(BonusObjectType)bonusType andPosition:(CGPoint)position
{
    return self;
}
//
-(void) update
{
    
}
//
-(void) releaseAgent
{
    [sprite removeFromParentAndCleanup:YES];
    [[Figure sharedPhysicsWorld] getPhysicsWorld]->DestroyBody(body);
}
@end
