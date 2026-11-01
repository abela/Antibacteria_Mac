//
//  KeyBind.h
//  Bacteria
//
//  Created by Giorgi Abelashvili on 2/12/13.
//  Copyright (c) 2013 __MyCompanyName__. All rights reserved.
//

#import <Foundation/Foundation.h>

@interface KeyBind : NSObject
{
    int keyCode;
    int keyQueue;
}
//
@property (nonatomic,assign) int keyCode;
@property (nonatomic,assign) int keyQueue;
//
@end
