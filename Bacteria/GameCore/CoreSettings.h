//
//  CoreSettings.h
//  Bacteria
//
//  Created by Giorgi Abelashvili on 2/10/13.
//  Copyright (c) 2013 __MyCompanyName__. All rights reserved.
//

#import <Foundation/Foundation.h>

@interface CoreSettings : NSObject
{
    //
    NSWindow *mainWindow;
    BOOL needGaussianBlurLoad;
    //
    CGPoint upperLeft;
    CGPoint upperRight;
    CGPoint bottomLeft;
    CGPoint bottomRight;
    float   aspectRatio;
    float   defaultScreenResolutionWidth;
}
@property (nonatomic,retain) NSWindow *mainWindow;
@property (nonatomic,assign) CGPoint upperLeft;
@property (nonatomic,assign) CGPoint upperRight;
@property (nonatomic,assign) CGPoint bottomLeft;
@property (nonatomic,assign) CGPoint bottomRight;
@property (nonatomic,assign) BOOL    needGaussianBlurLoad;
@property (nonatomic,assign) float   aspectRatio;
@property (nonatomic,assign) float   defaultScreenResolutionWidth;
+(CoreSettings*) sharedCoreSettings;
@end
