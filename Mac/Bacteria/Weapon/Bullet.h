//
//  Bullet.h
//  Game
//
//  Created by Giorgi Abelashvili on 10/19/11.
//  Copyright 2011 __MyCompanyName__. All rights reserved.
//

#import <Foundation/Foundation.h>
#import "Figure.h"

@interface Bullet : Figure {

	float speed;
	float damage;
	float bulletDelta;
	BulletType bulletType;
    CCSprite *glowSprite;
    id parentObject;
    int tag;
}
//
@property (nonatomic,assign) float speed;
@property (nonatomic,assign) float damage;
@property (nonatomic,assign) BulletType bulletType;
@property (nonatomic,assign) float bulletDelta;
@property (nonatomic,retain) CCSprite *glowSprite;
@property (nonatomic,retain) id parentObject;
@property (nonatomic,assign) int tag;
//
-(id) initAtPosition:(CGPoint)position
         andRotation:(float)angle
        andDirection:(CGPoint)direction
andCharacterVelocity:(CGPoint)characterVelocity;
//
-(id) initAtPosition:(CGPoint)position
         andRotation:(float)angle
        andDirection:(CGPoint)direction
       andStartDelta:(float)startDelta;
//
-(void) bulletLifeCycleTimer:(NSTimer*)timer;
//
@end
