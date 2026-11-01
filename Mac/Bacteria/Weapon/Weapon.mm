//
//  Weapon.mm
//  Game
//
//  Created by Giorgi Abelashvili on 10/19/11.
//  Copyright 2011 __MyCompanyName__. All rights reserved.
//

#import "Weapon.h"
#import "Bullet.h"
#import "Utils.h"
//
@implementation Weapon
//
@synthesize weaponType;
@synthesize bulletType;
@synthesize shootSpeed;
@synthesize parentObject;
//
static Weapon *_sharedWeapon = nil;
//
+(Weapon*)sharedWeapon
{
    if(!_sharedWeapon)
        _sharedWeapon = [[Weapon alloc] init];
	return _sharedWeapon;
}
//
-(id) init
{
    if((self = [super init]))
    {
        shootSpeed = 0.1f;
        return self;
    }
    return nil;
}
//
-(Bullet*) shootAtPosition:(CGPoint)position
          andWithDirection:(CGPoint)direction
            andRotateAngle:(float)rotateAngle
      andCharacterVelocity:(CGPoint)characterVelocity
{
    Bullet *bullet= [[Bullet alloc] initAtPosition:position
                                       andRotation:rotateAngle
                                      andDirection:direction
                              andCharacterVelocity:(CGPoint)characterVelocity];
    bullet.parentObject = self.parentObject;
    bullets.push_back(bullet);
    return bullet;
}
//
//
-(void) restart
{
    for(int i =0;i<(int)bullets.size();i++)
	{
        [bullets[i] resetBody];
        [bullets[i].sprite removeFromParentAndCleanup:YES];
        [bullets[i].glowSprite removeFromParentAndCleanup:YES];
        [bullets[i] release];
    }
    bullets.clear();
}
-(void) gameOver
{
    for(int i =0;i<(int)bullets.size();i++)
	{
        bullets[i].flag = kDealloc;
    }
}
//
//
-(Bullet*) shootAtPosition:(CGPoint)position
            andRotation:(float)angle
           andDirection:(CGPoint)direction
          andStartDelta:(float)startDelta
{
    Bullet *bullet =  [[Bullet alloc] initAtPosition:position
                                         andRotation:angle
                                        andDirection:direction
                                       andStartDelta:startDelta];
    bullet.parentObject = self.parentObject;
    bullets.push_back(bullet);
    return bullet;
}
//
-(void) updateBullets
{
    //
	for(int i =0;i<(int)bullets.size();i++)
	{
        Bullet *bullet = (Bullet*)bullets[i];
        bullet.speed*=[Utils timeScale];
		if(bullet.flag!=kDealloc)
		{
			[bullet updateSpritePositionWithBodyPosition];
		}
		//
		else {
			[bullet resetBody];
            bullet.sprite.opacity = 0;
            [bullet.sprite removeFromParentAndCleanup:YES];
            [bullet.glowSprite removeFromParentAndCleanup:YES];
			bullets.erase(bullets.begin()+i);
            [bullet release];
		}
	}
	//
}
//
-(void) removeAllBulletsInstantly
{
    for(int i =0;i<(int)bullets.size();i++)
	{
        [bullets[i] resetBody];
        bullets[i].sprite.opacity = 0;
        [bullets[i].sprite removeFromParentAndCleanup:YES];
        [bullets[i].glowSprite removeFromParentAndCleanup:YES];
        [bullets[i] release];
    }
    bullets.clear();
}
//
-(BOOL) isEmpty
{
    return (bullets.empty());
}
//
@end
