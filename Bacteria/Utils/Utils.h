//
//  Utils.h
//  Turtle
//
//  Created by Giorgi Abelashvili on 4/28/11.
//  Copyright 2011 __MyCompanyName__. All rights reserved.
//

#import <Foundation/Foundation.h>


@interface Utils : NSObject {
    
}
+(Utils*)sharedUtils;
+(float) timeScale;
+(int)getRandomNumber:(int)from to:(int)to;
+(void)annulizeArray:(NSMutableArray*)array withCapacity:(int)capacity;
+(BOOL) pointInRect:(CGPoint)point andRect:(CGRect)rect;
-(NSString *) hashString :(NSString *) data withSalt: (NSString *) salt;
//-(NSString *) genRandStringLength: (int) len;
//
-(float) calculateVectorLength:(CGPoint)vector;
-(CGPoint) vectorBetweenTwoPoint:(CGPoint)p1 :(CGPoint)p2;
-(CGPoint) vectorWithAngle:(float) angle;
//
-(double)GetRayCircleIntersect:(CGPoint)RayOrigin :(CGPoint)RayHeading :(CGPoint)CircleOrigin :(double)radius;
-(bool) DoRayCircleIntersect:(CGPoint) RayOrigin :(CGPoint) RayHeading :(CGPoint) CircleOrigin : (double)radius;
-(bool) LineIntersection2D:(CGPoint)A :(CGPoint)B :(CGPoint)C :(CGPoint)D;
-(bool)LineIntersection2D:(CGPoint)A :(CGPoint)B :(CGPoint)C :(CGPoint)D :(double &) dist;
-(bool)LineIntersection2D:(CGPoint)A :(CGPoint)B :(CGPoint)C :(CGPoint)D :(double&)dist :(CGPoint&) point;
//
-(CGPoint) scalePoint:(CGPoint)point;
@end
