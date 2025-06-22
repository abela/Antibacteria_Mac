//
//  PlistAction.m
//  PlistActions
//
//  Created by Giorgi Abelashvili on 1/8/11.
//  Copyright 2011 __MyCompanyName__. All rights reserved.
//

#import "PlistAction.h"


@implementation PlistAction

//
+ (NSString*)dataFilePath:(NSString*)name
{
	NSArray *paths = NSSearchPathForDirectoriesInDomains(NSDocumentDirectory, NSUserDomainMask, YES);
	
	NSString *documentsDirectory = [paths objectAtIndex:0];
	
	return [documentsDirectory stringByAppendingPathComponent:name];
}
//
+(void)addArrayToPlist:(NSMutableArray*)array atPath:(NSString*)path
{
	[array writeToFile:path atomically:YES];
	
	//NSArray *contents = [[NSArray alloc] initWithContentsOfFile:path];
}
//
+ (void)writeStringInPlist:(NSString*)str atFileName:(NSString*)fileName
{
	//get data file
	NSString * pathToWrite = [PlistAction dataFilePath:fileName];
	//
	NSMutableArray *array = [[NSMutableArray alloc] init];
	[array addObject:str];
	[array writeToFile:pathToWrite atomically:YES];
	[array release];
}
//
+ (NSString*)readStringFromPlist:(int)index atFileNamePath:(NSString*)path
{
	//NSString *filepath = [self dataFilePath];
	NSString *returnedString;
	if([[NSFileManager defaultManager] fileExistsAtPath:path])
	{
		NSArray *array = [[NSArray alloc] initWithContentsOfFile:path];
		returnedString = [array objectAtIndex:index];
		[array release];
	} 
	
	return returnedString;
}

@end
