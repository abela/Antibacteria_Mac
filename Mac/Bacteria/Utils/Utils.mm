//
//  Utils.m
//  Turtle
//
//  Created by Giorgi Abelashvili on 4/28/11.
//  Copyright 2011 __MyCompanyName__. All rights reserved.
//

#import "Utils.h"
#import "cocos2d.h"
#import "AiInput.h"
#import <CommonCrypto/CommonCrypto.h>
//
@interface Utils (PrivateMethods)
@end
//
@implementation Utils
//
Utils *_sharedUtils = nil;
//
+(bool) isEqual:(float)f1 :(float)f2
{
	return f1 == f2;
}
//
+(Utils*)sharedUtils
{
	@synchronized([Utils class])
	{
		if(!_sharedUtils)
			_sharedUtils = [[Utils alloc] init];
	}
	return _sharedUtils;
}
//
+(int)getRandomNumber:(int)from to:(int)to 
{
    return (int)from + arc4random() % (to-from+1);
}
//
+(void)annulizeArray:(NSMutableArray*)array withCapacity:(int)capacity
{
    for(int i = 0;i<capacity;i++)
    {
        [array addObject:[NSNumber numberWithInt:0]];
    }
}
//
-(NSString *) hashString :(NSString *) data withSalt: (NSString *) salt
{
    //
    const char *cKey  = [salt cStringUsingEncoding:NSUTF8StringEncoding];
    const char *cData = [data cStringUsingEncoding:NSUTF8StringEncoding];
    unsigned char cHMAC[CC_SHA256_DIGEST_LENGTH];
    CCHmac(kCCHmacAlgSHA256, cKey, strlen(cKey), cData, strlen(cData), cHMAC);
    
    NSString *hash;
    
    NSMutableString* output = [NSMutableString stringWithCapacity:CC_SHA256_DIGEST_LENGTH * 2];
    
    for(int i = 0; i < CC_SHA256_DIGEST_LENGTH; i++)
        [output appendFormat:@"%02x", cHMAC[i]];
    hash = output;
    return hash;
    
}
//
+(BOOL) pointInRect:(CGPoint)point andRect:(CGRect)rect
{
    return CGRectContainsPoint(rect,point);
}
//
-(float) calculateVectorLength:(CGPoint)vector
{
	return sqrt(pow(vector.x, 2) + pow(vector.y, 2));
}
//
-(CGPoint) vectorBetweenTwoPoint:(CGPoint)p1 :(CGPoint)p2
{
	return CGPointMake(p2.x - p1.x, p2.y - p1.y);
}
//
-(CGPoint) vectorWithAngle:(float) angle
{
    return ccp(sinf(angle),cosf(angle));
}
//
+(float) timeScale
{
    return [[[CCDirector sharedDirector] scheduler] timeScale];
}
//
-(CGPoint) scalePoint:(CGPoint)point
{
    CGSize designSize = {1024.0f,1024.0f};
    CGSize winSize = [CCDirector sharedDirector].winSize;
    CGSize scaleFactor = CGSizeMake(winSize.width / designSize.width,
                                    winSize.height / designSize.height);
    return CGPointMake(point.x * scaleFactor.width, point.y * scaleFactor.height);
}
//
-(double)GetRayCircleIntersect:(CGPoint)RayOrigin :(CGPoint)RayHeading :(CGPoint)CircleOrigin :(double)radius
{
	//
	CGPoint ToCircle = ccpSub(CircleOrigin,RayOrigin);
	double length = ccpLength(ToCircle);
	double v = ccpDot(ToCircle,RayHeading);
	double d = radius*radius - (length*length - v*v);
	//
	// If there was no intersection, return -1
	if (d < 0.0) return (-1.0);
	//
	// Return the distance to the [first] intersecting point
	return (v - sqrt(d));
}
//
//----------------------------- DoRayCircleIntersect --------------------------
-(bool) DoRayCircleIntersect:(CGPoint) RayOrigin :(CGPoint) RayHeading :(CGPoint) CircleOrigin : (double)radius
{
	
	CGPoint ToCircle = ccpSub(CircleOrigin,RayOrigin);
	double length = ccpLength(ToCircle);
	double v = ccpDot(ToCircle, RayHeading);
	double d = radius*radius - (length*length - v*v);
	
	// If there was no intersection, return -1
	return (d < 0.0);
}
//
//--------------------LineIntersection2D-------------------------
//
// Given 2 lines in 2D space AB, CD this returns true if an 
// intersection occurs.
//
//----------------------------------------------------------------- 

-(bool) LineIntersection2D:(CGPoint)A :(CGPoint)B :(CGPoint)C :(CGPoint)D
{
	double rTop = (A.y-C.y)*(D.x-C.x)-(A.x-C.x)*(D.y-C.y);
	double sTop = (A.y-C.y)*(B.x-A.x)-(A.x-C.x)*(B.y-A.y);
	
	double Bot = (B.x-A.x)*(D.y-C.y)-(B.y-A.y)*(D.x-C.x);
	
	if (Bot == 0)//parallel
	{
		return false;
	}
	
	double invBot = 1.0/Bot;
	double r = rTop * invBot;
	double s = sTop * invBot;
	
	if( (r > 0) && (r < 1) && (s > 0) && (s < 1) )
	{
		//lines intersect
		return true;
	}
	//
	//lines do not intersect
	return false;
}
//
//--------------------LineIntersection2D-------------------------
//
// Given 2 lines in 2D space AB, CD this returns true if an 
// intersection occurs and sets dist to the distance the intersection
//  occurs along AB
//
//----------------------------------------------------------------- 
//
-(bool)LineIntersection2D:(CGPoint)A :(CGPoint)B :(CGPoint)C :(CGPoint)D :(double &) dist
{
	
	double rTop = (A.y-C.y)*(D.x-C.x)-(A.x-C.x)*(D.y-C.y);
	double sTop = (A.y-C.y)*(B.x-A.x)-(A.x-C.x)*(B.y-A.y);
	//
	double Bot = (B.x-A.x)*(D.y-C.y)-(B.y-A.y)*(D.x-C.x);
	//
	if (Bot == 0)//parallel
	{
		if ([Utils isEqual:rTop :0] && [Utils isEqual:sTop :0])
		{
			return true;
		}
		return false;
	}
	
	double r = rTop/Bot;
	double s = sTop/Bot;
	
	if( (r > 0) && (r < 1) && (s > 0) && (s < 1) )
	{
		
		dist = ccpDistance(A,B) * r;
		return true;
	}
	//
	else
	{
		dist = 0;
		return false;
	}
}
//
//
//-------------------- LineIntersection2D-------------------------
//
// Given 2 lines in 2D space AB, CD this returns true if an 
// intersection occurs and sets dist to the distance the intersection
//  occurs along AB. Also sets the 2d vector point to the point of
//  intersection
//----------------------------------------------------------------- 
-(bool)LineIntersection2D:(CGPoint)A :(CGPoint)B :(CGPoint)C :(CGPoint)D :(double&)dist :(CGPoint&) point
{
	
	double rTop = (A.y-C.y)*(D.x-C.x)-(A.x-C.x)*(D.y-C.y);
	double rBot = (B.x-A.x)*(D.y-C.y)-(B.y-A.y)*(D.x-C.x);
	//
	double sTop = (A.y-C.y)*(B.x-A.x)-(A.x-C.x)*(B.y-A.y);
	double sBot = (B.x-A.x)*(D.y-C.y)-(B.y-A.y)*(D.x-C.x);
	
	if ( (rBot == 0) || (sBot == 0))
	{
		//lines are parallel
		return false;
	}
	
	double r = rTop/rBot;
	double s = sTop/sBot;
	
	if( (r > 0) && (r < 1) && (s > 0) && (s < 1) )
	{
		dist = ccpDistance(A,B) * r;
		//point = A + r*(B - A);
		CGPoint v1 = ccpSub(B, A);
		v1 = ccp(r*v1.x,r*v1.y);
		point = ccpAdd(A, v1);
		return true;
	}
	//
	else
	{
		dist = 0;
		return false;
	}
}
//
//
//----------------------- ObjectIntersection2D ---------------------------
//
//  tests two polygons for intersection. *Does not check for enclosure*
//------------------------------------------------------------------------
/*-(bool) ObjectIntersection2D:(NSMutableArray *)gameWorldObjects1 :(NSMutableArray *)gameWorldObjects2;
{
	//test each line segment of object1 against each segment of object2
	for (int r=0; r<(int)[gameWorldObjects1 count]; ++r)
	{
		for (int t=0; t<(int)[gameWorldObjects2 count]; ++t)
		{
			CGPoint pos1 = ((GameWorldObject*)([gameWorldObjects2 objectAtIndex:t])).sprite.positionInPixels;
			CGPoint pos2 = ((GameWorldObject*)([gameWorldObjects2 objectAtIndex:(t+1)])).sprite.positionInPixels;
			CGPoint pos3 = ((GameWorldObject*)([gameWorldObjects1 objectAtIndex:r])).sprite.positionInPixels;
			CGPoint pos4 = ((GameWorldObject*)([gameWorldObjects1 objectAtIndex:(r+1)])).sprite.positionInPixels;
			//
			if([self LineIntersection2D:pos1 :pos2 :pos3 :pos4] == true)
				return true;
		}
	}
	return false;
}*/
//
//
@end
