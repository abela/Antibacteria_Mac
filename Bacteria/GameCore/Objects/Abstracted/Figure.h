//
//  Vehicle.h
//  Tanks
//
//  Created by Giorgi Abelashvili on 11/17/10.
//  Copyright 2010 __MyCompanyName__. All rights reserved.
//

#import <Foundation/Foundation.h>
#import "cocos2d.h"
#import "Box2D.h"
#import "Physics.h"

@interface Figure : NSObject {

	//physics world
	Physics *physics;
	//Sprite batch
	CCSpriteBatchNode *batchNode;
	//animation
	CCAnimation *animation;
	//vehicle id
	int id;  //reserved
	//sprite
	CCSprite *sprite;
	//physics body
	b2Body *body;
	//width
	float width;
	//height
	float height;
	//float
	CGPoint startPosition;
    CGPoint lastPosition; 
	//object moving flag
	ObjectMovingFlag objectMovingFlag;
	//collide direction
	ObjectMovingFlag collisionDirectionFlag;
	//direction
	ObjectMovingFlag currentDirection;
	//object flag
	ObjectType type;
	//objec event
	ObjectEvent flag;
	//animation frame counter
	int frameAnimationCounter;
	//is collided
	BOOL isCollided;
    //
    float objectSize;
    //
}

//properties
@property (nonatomic, retain) CCSprite *sprite;

@property (readonly) b2Body *body;

@property (nonatomic, assign) ObjectEvent flag;

@property (nonatomic, assign) ObjectType type;

@property (nonatomic, assign) ObjectMovingFlag objectMovingFlag;
@property (nonatomic, assign) ObjectMovingFlag currentDirection;
@property (nonatomic, assign) ObjectMovingFlag collisionDirectionFlag;
@property (nonatomic, assign) CGPoint startPosition;
@property (nonatomic, assign) CGPoint lastPosition;
@property (nonatomic, retain) CCTMXTiledMap *tileMap;
@property (nonatomic, retain) CCTMXLayer *layer;
//
@property (nonatomic,retain) CCSpriteBatchNode *batchNode;

@property (nonatomic , assign) int frameAnimationCounter;

@property (nonatomic, assign) BOOL isCollided;

@property (nonatomic,assign) float objectSize;

@property (nonatomic,assign) float width;
@property (nonatomic,assign) float height;
//functions
-(void)initAtPosition:(CGPoint)position;
//
+(Physics*)sharedPhysicsWorld;
//create new Dinamyc body
- (void) createNewBodyAt:(CGPoint)position andDimension:(CGPoint)dimension;
//create new ground body
-(void) createNewGroundBodyAt:(CGPoint)position andDimension:(CGPoint)dimension;
//// update an sprite position
- (void)updatePhysicsBodyPosition:(CGPoint)position;
//update object rotation
- (void)updateRotation:(float)rotation;
//- (void)animateObject;
-(void) resetBody;
//
-(void) updateSpritePositionWithBodyPosition;
//
@end






